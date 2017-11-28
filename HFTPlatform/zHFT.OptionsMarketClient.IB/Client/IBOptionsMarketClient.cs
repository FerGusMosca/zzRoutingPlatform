using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IBApi;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.IB.Common;
using zHFT.MarketClient.IB.Common.Converters;
using zHFT.OptionsMarketClient.BusinessEntities;
using zHFT.OptionsMarketClient.Common.Interfaces;
using zHFT.OptionsMarketClient.DataAccessLayer.Managers;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.OptionsMarketClient.IB.Client
{
    public class IBOptionsMarketClient : IBMarketClientBase
    {
        #region Private  Consts

        private string _US_SMART_EXCHANGE = "SMART";

        private string _US_PRIMARY_EXCHANGE = "ISLAND";

        private string _OPTIONS_SECURITY_TYPE = "OPT";

        private string _STOCK_SECURITY_TYPE = "STK";

        private int MaxTimeForContractMarketDataRecovery = 10;//seconds

        private int _MAX_PREV_DAYS_TO_FIND_OPTIONS = 5;

        private int _SECURITY_TYPE_STOCK_PREFIX = 10000;//10.000 options maximun on the same time

        private int _MAX_CONTRACTS_PER_SECOND = 100;

        private decimal _THRESHOLD_FOR_CONTRACTS_WITH_MARKET_DATA = 80;//When we get 80 pct of data we consider that we it is enough

        #endregion

        #region Private Attributes

        public IConfiguration Config { get; set; }

        protected OptionsMarketClient.Common.Configuration.Configuration OMCConfiguration
        {
            get { return (OptionsMarketClient.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected StockManager StockManager { get; set; }

        protected OptionsManager OptionsManager { get; set; }

        protected DailyOptionManager DailyOptionManager { get; set; }

        protected Thread PublishThread { get; set; }

        protected Thread CleanOldSecuritiesThread { get; set; }

        protected Thread RecoverSecurityOptionsThread { get; set; }

        protected Thread ContractMarketDataDownloaderThread { get; set; }

        protected Thread ContractMarketDataEvaluator { get; set; }

        protected Thread ContractRealTimeMarketDataDownloader { get; set; }

        protected Thread ManualContractMarketDataDownloader { get; set; }

        private Dictionary<int, Security> ActiveSecurities { get; set; }

        private Dictionary<string, bool> SecuritiesToDownload { get; set; }

        private Dictionary<string, bool> ContractsMarketData { get; set; }

        private Dictionary<int, Security> ActiveOptions { get; set; }

        private IContractFilter ContractFilter { get; set; }

        private bool RunAdHocRecovery { get; set; }

        private DateTime LastRecovery { get; set; }

        private DateTime LastMarketDataRequest { get; set; }

        private DateTime? DailyOptionsOperatingDate { get; set; }

        protected object tLock = new object();

        #endregion

        #region Constructors

        public IBOptionsMarketClient() { }

        #endregion

        #region Private Methods

        private List<DailyOption> RecoverLastDailyOptionsAvailable()
        {
            for (int i = 0; i <= _MAX_PREV_DAYS_TO_FIND_OPTIONS; i++)
            {
                DateTime dateToUse = DateTime.Now.AddDays(i * (-1));
                List<DailyOption> optionsToProcess = DailyOptionManager.GetLatestToProcess(dateToUse);

                if (optionsToProcess != null && optionsToProcess.Count() > 0)
                {
                    DailyOptionsOperatingDate = dateToUse.Date;
                    return optionsToProcess;
                }

            }
            return new List<DailyOption>();

        }

        protected bool IsMarketTime()
        {
            TimeSpan start = TimeSpan.Parse(OMCConfiguration.MarketStartTime);

            TimeSpan end = TimeSpan.Parse(OMCConfiguration.MarketEndTime);

            DateTime today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

            DateTime startDate = today.Add(start);
            DateTime endDate = today.Add(end);


            return (DateTime.Compare(startDate, DateTime.Now) <= 0 && DateTime.Compare(endDate, DateTime.Now) >= 0);
        }

        private void CleanAllDictionaries()
        {
            ActiveOptions.Clear();
            ActiveSecurities.Clear();
            SecuritiesToDownload.Clear();
            ContractsMarketData.Clear();
        }

        private void ExpireOldContracts()
        {
            List<Stock> stocksToMonitor = StockManager.GetAll();

            foreach (Stock sec in stocksToMonitor)
            {
                List<Option> options = OptionsManager.GetAllOptionsBySymbol(sec.Symbol);

                foreach (Option opt in options)
                {
                    if (DateTime.Compare(opt.MaturityDate.Date, DateTime.Now.Date) <= 0)
                    {
                        opt.Expired = true;
                        OptionsManager.Persist(opt);
                    }
                }
            }
        }

        private bool EvalEnoughContractsWithMarketData(List<Option> options)
        {
            Security secWithMarketData = RecoverMainStockWithMarketData(options.FirstOrDefault());

            if (secWithMarketData != null && secWithMarketData.MarketData != null && secWithMarketData.MarketData.ClosingPrice != null
                 && secWithMarketData.MarketData.TradeVolume != null)
            {


                bool enoughRecovered = true;
                int count = 0;

                foreach (Option opt in options)
                {
                    if (ContractsMarketData.ContainsKey(opt.Symbol))
                    {
                        bool marketDataRecoveredForOption = ContractsMarketData[opt.Symbol];
                        if (marketDataRecoveredForOption)
                        {
                            count++;
                        }

                    }
                }

                decimal pctRecovered = (Convert.ToDecimal(count) / Convert.ToDecimal(options.Count)) * 100;

                enoughRecovered = pctRecovered > _THRESHOLD_FOR_CONTRACTS_WITH_MARKET_DATA;

                return enoughRecovered;
            }
            else
                return false;
        }

        private Security RecoverMainStockWithMarketData(Option option)
        {

            foreach (Security sec in ActiveSecurities.Values)
            {
                if (sec.Symbol == option.SymbolSfx)
                    return sec;

            }

            DoLog(string.Format("@{0}  - Could not find security market data for symbol {1} ",
                                OMCConfiguration.Name, option.SymbolSfx),
                                Main.Common.Util.Constants.MessageType.Error);

            return null;
        }

        private List<Option> RecoverAllContractsWithMarketData(List<Option> options)
        {
            List<Option> optionsWithMarketData = new List<Option>();
            foreach (Option opt in options)
            {
                if (ContractsMarketData.ContainsKey(opt.Symbol))
                {
                    bool marketDataRecoveredForOption = ContractsMarketData[opt.Symbol];
                    if (marketDataRecoveredForOption)
                    {
                        optionsWithMarketData.Add(opt);
                    }
                }

                //We update the option market data, base on what we received from IB
                if (opt.ReqId.HasValue && ActiveOptions.Keys.Contains(opt.ReqId.Value))
                {
                    Security optSec = ActiveOptions[opt.ReqId.Value];

                    opt.TradeVolume = optSec.MarketData.TradeVolume;
                    opt.ClosingPrice = optSec.MarketData.ClosingPrice;
                }
            }

            return optionsWithMarketData;
        }

        private void RequestContractDetails(string symbol, string exchange, string currency, int index)
        {
            Contract contract = new Contract();
            contract.Symbol = symbol;
            contract.SecType = _OPTIONS_SECURITY_TYPE;
            contract.Exchange = exchange;
            contract.Currency = currency;
            ClientSocket.reqContractDetails(index, contract);
        }

        private void RequestStockMarketData(string symbol, string exchange, string currency, int index, bool snapshot)
        {
            Contract ibContract = new Contract()
            {

                Symbol = symbol,
                LocalSymbol = symbol,
                SecType = _STOCK_SECURITY_TYPE,
                Exchange = exchange,
                Currency = currency,
                PrimaryExch = _US_PRIMARY_EXCHANGE,
            };

            ClientSocket.reqMktData(index, ibContract, "", snapshot, null);
            
        }

        private void RequestOptionMarketData(string symbolSfx,string symbol, string exchange, string currency, int index, bool snapshot)
        {

            Contract ibContract = new Contract()
            {

                Symbol = symbolSfx,
                LocalSymbol = symbol,
                SecType = _OPTIONS_SECURITY_TYPE,
                Exchange = exchange,
                Currency = currency,
                //PrimaryExch = _US_PRIMARY_EXCHANGE,
            };

            ClientSocket.reqMktData(index, ibContract, "", snapshot, null);

        }

        private bool StillSecuritiesToDownload()
        {
            lock (tLock)
            {
                bool stillToDownload = SecuritiesToDownload.Values.Any(x => x == false)
                                        || SecuritiesToDownload.Values.Count() == 0;
                return stillToDownload;
            }
        }

        private void EvalRequestMarketDataForStock(string symbol)
        {
            if (!ActiveSecurities.Values.Any(x => x.Symbol == symbol))
            {
                int stockIndex = _SECURITY_TYPE_STOCK_PREFIX + ActiveSecurities.Keys.Count();

                Security security = new Security()
                {
                    Symbol = symbol,
                    SecType = SecurityType.CS,
                    Exchange = OMCConfiguration.Exchange,
                    Currency = OMCConfiguration.Currency

                };

                RequestStockMarketData(security.Symbol, security.Exchange, security.Currency, stockIndex, false);
                ActiveSecurities.Add(stockIndex, security);
            }
        }

        private void EvalRequestMarketDataForOptions(List<DailyOption> optionsToProcess)
        {
            int i = 0;
            int j = 0;
            int maxContractsPersSecond = OMCConfiguration.MaxContractsInSession - ActiveSecurities.Keys.Count();

            foreach (DailyOption dailyOpt in optionsToProcess)
            {
                Option opt = OptionsManager.GetActiveOptionBySymbol(dailyOpt.Option.Symbol);

                if (opt != null)
                {

                    Security optSec = new Security()
                    {
                        Symbol = opt.Symbol,
                        SymbolSfx = opt.SymbolSfx,
                        SecType = SecurityType.OPT,
                        Exchange = OMCConfiguration.Exchange,
                        Currency = OMCConfiguration.Currency
                    };

                    RequestOptionMarketData(opt.SymbolSfx,opt.Symbol, OMCConfiguration.Exchange, OMCConfiguration.Currency, i, false);
                    ActiveOptions.Add(i, optSec);
                    i++;
                    j++;

                    if (j >= maxContractsPersSecond)
                    {
                        Thread.Sleep(1200);//MaxContractsInSession per second
                        j = 0;
                    }
                }
                else
                {
                    DoLog(string.Format("@{0} @DoContractRealTimeMarketDataDownloader: Could not retrieve option data for symbol {0} ",
                                        OMCConfiguration.Name, dailyOpt.Option.Symbol), Main.Common.Util.Constants.MessageType.Error);
                }
            }
        
        }

        #endregion

        #region Protected Methods

        protected override IConfiguration GetConfig() { return Config; }

        protected IOptionConverter OptionConverter { get; set; }

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new OptionsMarketClient.Common.Configuration.Configuration().GetConfiguration<OptionsMarketClient.Common.Configuration.Configuration>(configFile, noValueFields);
        }

        protected override void ProcessField(string ev, int tickerId, int field, double value)
        {
            try
            {
              
                if (tickerId >= _SECURITY_TYPE_STOCK_PREFIX)
                {
                    if (ActiveSecurities.ContainsKey(tickerId))
                    {

                        Security sec = ActiveSecurities[tickerId];
                        sec.MarketData.MDLocalEntryDate = DateTime.Now;
                        SecurityConverter.AssignValueBasedOnField(sec, field, value);

                    }
                    else
                        DoLog(string.Format("Unknown tickerId event for stock: event={0} tickerId={1} field={2} ",
                                ev, tickerId, field), Main.Common.Util.Constants.MessageType.Error);
                }
                else
                {
                    if (ActiveOptions.ContainsKey(tickerId))
                    {

                        Security opt = ActiveOptions[tickerId];
                        opt.MarketData.MDLocalEntryDate = DateTime.Now;
                        SecurityConverter.AssignValueBasedOnField(opt, field, value);

                    }
                    else
                        DoLog(string.Format("Unknown tickerId event for option: event={0} tickerId={1} field={2} ",
                                ev, tickerId, field), Main.Common.Util.Constants.MessageType.Error);
                    
                    
                }

                

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error processing event: event={0} tickerId={1} field={2} error={3} ",
                    ev, tickerId, field, ex.Message), Main.Common.Util.Constants.MessageType.Error);

            }

        }

        protected override void ProcessField(string ev, int tickerId, int field, int value)
        {
            try
            {
              
                if (tickerId >= _SECURITY_TYPE_STOCK_PREFIX)
                {
                    if (ActiveSecurities.ContainsKey(tickerId))
                    {

                        Security sec = ActiveSecurities[tickerId];
                        sec.MarketData.MDLocalEntryDate = DateTime.Now;
                        SecurityConverter.AssignValueBasedOnField(sec, field, value);

                    }
                    else
                        DoLog(string.Format("Unknown tickerId event for stock: event={0} tickerId={1} field={2} ",
                                ev, tickerId, field), Main.Common.Util.Constants.MessageType.Error);
                }
                else
                {
                    if (ActiveOptions.ContainsKey(tickerId))
                    {

                        Security opt = ActiveOptions[tickerId];
                        opt.MarketData.MDLocalEntryDate = DateTime.Now;
                        SecurityConverter.AssignValueBasedOnField(opt, field, value);

                    }
                    else
                        DoLog(string.Format("Unknown tickerId event for option: event={0} tickerId={1} field={2} ",
                                ev, tickerId, field), Main.Common.Util.Constants.MessageType.Error);


                }

               

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error processing event: event={0} tickerId={1} field={2} error={3} ",
                    ev, tickerId, field, ex.Message), Main.Common.Util.Constants.MessageType.Error);

            }

        }

        protected override void ProcessField(string ev, int tickerId, int field, string value)
        {
            try
            {
                
                if (tickerId >= _SECURITY_TYPE_STOCK_PREFIX)
                {
                    if (ActiveSecurities.ContainsKey(tickerId))
                    {

                        Security sec = ActiveSecurities[tickerId];
                        sec.MarketData.MDLocalEntryDate = DateTime.Now;
                        SecurityConverter.AssignValueBasedOnField(sec, field, value);

                    }
                    else
                        DoLog(string.Format("Unknown tickerId event for stock: event={0} tickerId={1} field={2} ",
                                ev, tickerId, field), Main.Common.Util.Constants.MessageType.Error);
                }
                else
                {
                    if (ActiveOptions.ContainsKey(tickerId))
                    {

                        Security opt = ActiveOptions[tickerId];
                        opt.MarketData.MDLocalEntryDate = DateTime.Now;
                        SecurityConverter.AssignValueBasedOnField(opt, field, value);

                    }
                    else
                        DoLog(string.Format("Unknown tickerId event for option: event={0} tickerId={1} field={2} ",
                                ev, tickerId, field), Main.Common.Util.Constants.MessageType.Error);

                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error processing event: event={0} tickerId={1} field={2} error={3} ",
                    ev, tickerId, field, ex.Message), Main.Common.Util.Constants.MessageType.Error);

            }

        }

        public override void contractDetails(int reqId, ContractDetails contractDetails)
        {
            DoLog(string.Format("contractDetails: reqId={0} contractDetails={1}",
                                reqId,
                                contractDetails.ToString()), Main.Common.Util.Constants.MessageType.Information);
            ProcessContractDetails(reqId, contractDetails);
        }

        public override void contractDetailsEnd(int reqId)
        {
            DoLog(string.Format("contractDetailsEnd: reqId={0}",
                                reqId), Main.Common.Util.Constants.MessageType.Information);

            try
            {
                lock (tLock)
                {
                    Security security = ActiveSecurities[_SECURITY_TYPE_STOCK_PREFIX + reqId];

                    if (security != null)
                    {
                        try
                        {
                            ContractMarketDataDownloaderThread = new Thread(DoContractMarketDataDownload);
                            ContractMarketDataDownloaderThread.Start(security);
                        }
                        catch (Exception ex)
                        {
                            DoLog(string.Format("@{0}- Critical error creatin thread ContractMarketDataDownloaderThread: {1}",
                                                OMCConfiguration.Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                        }
                    }
                    else
                    {
                        DoLog(string.Format("@{0}- Critical error recovering Security for reqId={1}: There was not a Security for such request Id",
                                            OMCConfiguration.Name, reqId), Main.Common.Util.Constants.MessageType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error @contractDetailsEnd: reqId={0} error={1} ",
                                    reqId,  ex.Message), Main.Common.Util.Constants.MessageType.Error);
            
            }

        }

        public override void tickSnapshotEnd(int tickerId)
        {
            try
            {
                DoLog(string.Format("tickSnapshotEnd: tickerId={0}",
                                tickerId), Main.Common.Util.Constants.MessageType.Information);

                if (tickerId  < _SECURITY_TYPE_STOCK_PREFIX)
                {
                    if (ActiveOptions.Keys.Contains(tickerId))
                    {
                        Security opt = ActiveOptions[tickerId];

                        if (ContractsMarketData.Keys.Contains(opt.Symbol))
                        {
                            ContractsMarketData[opt.Symbol] = true;
                        }
                        else
                            DoLog(string.Format("tickSnapshotEnd: Could not find option @ContractsMarketData for  tickerId {0}",
                                         tickerId), Main.Common.Util.Constants.MessageType.Error);

                    }
                    else
                        DoLog(string.Format("tickSnapshotEnd: Could not find option @ActiveOptions for  tickerId {0}",
                                         tickerId), Main.Common.Util.Constants.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("tickSnapshotEnd: Critical errpr for  tickerId {0}:{1}",
                    tickerId, ex.Message + "-" + (ex.InnerException != null ? ex.InnerException.Message : "")), Main.Common.Util.Constants.MessageType.Error);
            
            }

        }

        protected void DoContractMarketDataEvaluator(object param)
        {
            Security security = (Security)((object[])param)[0];
            List<Option> options = (List<Option>)((object[])param)[1];

            try
            {
                DateTime timestamp = DateTime.Now;
                bool run = options.Count() > 0;
                //MaxTimeForContractMarketDataRecovery = Math.Abs(options.Count / _MAX_CONTRACTS_PER_SECOND) +1 ;
                MaxTimeForContractMarketDataRecovery = Int32.MaxValue;

                while (run)
                {
                    bool enoughRecovered = false;

                    lock (tLock)
                    {
                        enoughRecovered = EvalEnoughContractsWithMarketData(options);
                    }

                    Thread.Sleep(1000);//1 second

                    TimeSpan elapsed = DateTime.Now - timestamp;
                    if (enoughRecovered || elapsed.TotalSeconds > MaxTimeForContractMarketDataRecovery)
                    {
                        lock (tLock)//Lo que se recupero se recupero y se trabaja con eso
                        {
                            if (options.Count() > 0)
                            {
                                Security secWithMarketData = RecoverMainStockWithMarketData(options.FirstOrDefault());
                                List<Option> optionsWithMarketData = RecoverAllContractsWithMarketData(options);

                                //We keep only the contracts we want to use
                                List<Option> filteredOptions = ContractFilter.FilterContracts(secWithMarketData, optionsWithMarketData, OMCConfiguration);

                                foreach (Option filteredOpt in filteredOptions.Take(OMCConfiguration.MaxContractsPerSecurity))
                                {

                                    DailyOption dailyOpt = new DailyOption()
                                    {
                                        Option = new Option() { Symbol = filteredOpt.Symbol, SymbolSfx = secWithMarketData.Symbol },
                                        Date = DateTime.Now.Date,
                                        Processed = true
                                    };
                                    DailyOptionsOperatingDate = DateTime.Now.Date;
                                    DailyOptionManager.Persist(dailyOpt);
                                }
                            }

                            SecuritiesToDownload[security.Symbol] = true;
                            run = false;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                SecuritiesToDownload[security.Symbol] = true;
                DoLog(string.Format("@{0}  - Critical Error On market data evaluator for symbol {1}: {2} ",
                                                    OMCConfiguration.Name,
                                                    security != null ? security.Symbol : "unknown symbol",
                                                    ex.Message + " - " + (ex.InnerException != null ? ex.InnerException.Message : "")),
                                                    Main.Common.Util.Constants.MessageType.Error);

            }
        }

        protected void DoManualContractMarketDataDownloader(object param)
        {

            bool run = true;
            while (run)
            {
                lock (tLock)
                {
                    try
                    {
                        if (DailyOptionsOperatingDate.HasValue)
                        {
                            List<DailyOption> newDailyOptions = DailyOptionManager.GetManualContracts(DailyOptionsOperatingDate.Value.Date);

                            if (newDailyOptions != null)
                            {
                                foreach (DailyOption newOpt in newDailyOptions)
                                {
                                    DoLog(string.Format("@{0} @DoManualContractMarketDataDownloader: Incorporating new contract {1} ",
                                                             OMCConfiguration.Name, newOpt.Option.Symbol),
                                                             Main.Common.Util.Constants.MessageType.Information);

                                    Security optSec = new Security()
                                    {
                                        Symbol = newOpt.Option.Symbol,
                                        SymbolSfx = newOpt.Option.SymbolSfx,
                                        SecType = SecurityType.OPT,
                                        Exchange = OMCConfiguration.Exchange,
                                        Currency = OMCConfiguration.Currency
                                    };

                                    int index = ActiveOptions.Keys.Count();
                                    ActiveOptions.Add(index, optSec);

                                    RequestOptionMarketData(newOpt.Option.SymbolSfx, newOpt.Option.Symbol, optSec.Exchange, optSec.Currency, index, false);

                                    newOpt.Option.ReqId = index;

                                    newOpt.Processed = true;
                                    DailyOptionManager.Persist(newOpt);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("@{0}  - Critical Error @DoManualContractMarketDataDownloader : {1} ",
                                                    OMCConfiguration.Name,
                                                    ex.Message + " - " + (ex.InnerException != null ? ex.InnerException.Message : "")),
                                                    Main.Common.Util.Constants.MessageType.Error);
                        run = false;
                    }
                }
                Thread.Sleep(5000);
            }
        
        }

        protected void DoContractMarketDataDownload(object param)
        {
            Security sec = (Security)param;

            lock (tLock)
            {
                List<Option> options = OptionsManager.GetAllActiveOptionsBySymbol(sec.Symbol);
                int i = 0;
                int maxContractsPersSecond = OMCConfiguration.MaxContractsInSession - ActiveSecurities.Keys.Count();
                Thread.Sleep(1200);
                foreach (Option opt in options)
                {
                    try
                    {

                        if (ContractsMarketData.ContainsKey(opt.Symbol))
                            ContractsMarketData[opt.Symbol] = false;
                        else
                            ContractsMarketData.Add(opt.Symbol, false);

                        Security optSec = new Security()
                        {
                            Symbol = opt.Symbol,
                            SecType = SecurityType.OPT,
                            Exchange = OMCConfiguration.Exchange,
                            Currency = OMCConfiguration.Currency
                        };

                        int index = ActiveOptions.Keys.Count();
                        ActiveOptions.Add(index, optSec);

                        RequestOptionMarketData(opt.SymbolSfx,opt.Symbol, sec.Exchange, sec.Currency, index, true);
                        i++;
                        opt.ReqId = index;


                        if (i >= maxContractsPersSecond)
                        {
                            Thread.Sleep(1200);//MaxContractsInSession per second
                            i = 0;
                        }

                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("@{0}  - Critical Error Requesting Snapshot Market Data for symbol {1}: {2} ",
                                                    OMCConfiguration.Name,
                                                    opt.Symbol,
                                                    ex.Message + " - " + (ex.InnerException != null ? ex.InnerException.Message : "")),
                                                    Main.Common.Util.Constants.MessageType.Error);
                    }
                }

                ContractMarketDataEvaluator = new Thread(DoContractMarketDataEvaluator);
                ContractMarketDataEvaluator.Start(new object[] { sec, options });
            }
        }

        

        protected void DoContractRealTimeMarketDataDownloader(object param)
        {
            bool run=true;
            while (run)
            {
                if (IsMarketTime())
                {
                    if (DateTime.Compare(LastMarketDataRequest.Date, DateTime.Now.Date) < 0)
                    {
                        //We process the latest for today
                        List<DailyOption> optionsToProcess = RecoverLastDailyOptionsAvailable();

                        if (optionsToProcess != null && optionsToProcess.Count() > 0 && !OMCConfiguration.ForceContractRecovery)
                        {
                            lock (tLock)
                            {
                                ActiveOptions.Clear();
                                ActiveSecurities.Clear();

                                if(!ClientSocket.IsConnected())
                                    ClientSocket.eConnect(OMCConfiguration.IP, OMCConfiguration.Port, OMCConfiguration.IdIBClient);

                                Thread.Sleep(1000 * 5);//5 seconds
                                optionsToProcess = optionsToProcess.Take(OMCConfiguration.MaxContractsInSession).ToList();

                                foreach (DailyOption option in optionsToProcess)
                                    EvalRequestMarketDataForStock(option.Option.SymbolSfx);

                                EvalRequestMarketDataForOptions(optionsToProcess);
                                LastMarketDataRequest = DateTime.Now;
                            }
                        }
                        else
                        {
                            RunAdHocRecovery = true;
                            CleanAllDictionaries();
                            LastRecovery = DateTime.Now.Date;
                            Thread RecoverSecurityOptionsThread = new Thread(DoRecoverSecurityOptions);
                            RecoverSecurityOptionsThread.Start();

                            while (StillSecuritiesToDownload())
                                Thread.Sleep(1000);//1 second

                            OMCConfiguration.ForceContractRecovery = false;
                            continue;//we try to process again the DailyOptionManager.GetLatestToProcess
                        }
                    }
                    else
                        Thread.Sleep(1000 * 60);//1 minute
                }
                else
                {
                    if (DateTime.Compare(LastRecovery, DateTime.Now.Date) < 0)
                    {
                        ExpireOldContracts();
                        CleanAllDictionaries();
                        LastRecovery = DateTime.Now.Date;
                        Thread RecoverSecurityOptionsThread = new Thread(DoRecoverSecurityOptions);
                        RecoverSecurityOptionsThread.Start();
                    }

                    //we have to wait for the process of RecoverSecurityOptions to do its work
                    Thread.Sleep(1 * 60 * 1000);//1 minute
                }
            }
        }

        protected void DoRecoverSecurityOptions()
        {
            int i = 0;
            
            if (!IsMarketTime() || RunAdHocRecovery)
            {
                ClientSocket.eConnect(OMCConfiguration.IP, OMCConfiguration.Port, OMCConfiguration.IdIBClient);
                RunAdHocRecovery = false;
                lock (tLock)
                {
                    try
                    {
                        List<Stock> stocksToMonitor = StockManager.GetAll();
                        if (stocksToMonitor != null)
                        {
                            DoLog(string.Format("@{0} Updating options for {1} stocks ", OMCConfiguration.Name, stocksToMonitor.Count()), Main.Common.Util.Constants.MessageType.Information);

                            foreach (Stock stock in stocksToMonitor)
                            {
                                Security security = new Security()
                                {
                                    Symbol = stock.Symbol,
                                    SecType = SecurityType.CS,
                                    Exchange = OMCConfiguration.Exchange,
                                    Currency = OMCConfiguration.Currency

                                };

                                RequestContractDetails(stock.Symbol, OMCConfiguration.Exchange, OMCConfiguration.Currency, i);
                                RequestStockMarketData(stock.Symbol, OMCConfiguration.Exchange, OMCConfiguration.Currency, _SECURITY_TYPE_STOCK_PREFIX + i, true);

                                ActiveSecurities.Add(_SECURITY_TYPE_STOCK_PREFIX + i, security);
                                SecuritiesToDownload.Add(security.Symbol, false);
                                i++;

                            }

                        }
                        else
                            DoLog(string.Format("@{0} There are no stocks to monitor. The system will not be able to process anything ", OMCConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);

                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("@{0}  - Critical Error Updating options: {1} ",
                                            OMCConfiguration.Name,
                                            ex.Message + " - " + (ex.InnerException != null ? ex.InnerException.Message : "")),
                                            Main.Common.Util.Constants.MessageType.Error);
                    }
                }

                Thread.Sleep(OMCConfiguration.SecurityUpdateInHours * 60 * 60 * 1000);//hours to milliseconds
            }
            else
            {

                CleanAllDictionaries();
            }
            
        }

        protected void ProcessContractDetails(int reqId, ContractDetails contractDetails)
        {

            try
            {
                lock (tLock)
                {
                    if (   contractDetails.Summary != null 
                        && contractDetails.Summary.LocalSymbol != null
                        && contractDetails.Summary.SecType==_OPTIONS_SECURITY_TYPE)
                    {
                        string symbol = contractDetails.Summary.LocalSymbol;
                        Option opt = OptionsManager.GetActiveOptionBySymbol(symbol);

                        if (opt == null)
                        {

                            DoLog(string.Format("@{0}  - Inserting new option: {1} ", OMCConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);


                            Option newOption = new Option()
                            {
                                Symbol = contractDetails.Summary.LocalSymbol,
                                SymbolSfx = contractDetails.Summary.Symbol,
                                StrikeMultiplier = Option.ProcessMultiplier(contractDetails.Summary.Multiplier),
                                PutOrCall = Option.ProcessPutOrCall(contractDetails.Summary.Right),
                                StrikePrice = contractDetails.Summary.Strike,
                                StrikeCurrency = contractDetails.Summary.Currency,
                                MaturityMonthYear = contractDetails.Summary.Expiry,
                                MaturityDate = OptionConverter.ExctractMaturityDateFromSymbol(contractDetails.Summary.LocalSymbol),
                                Currency = contractDetails.Summary.Currency,
                                SecurityExchange = contractDetails.Summary.Exchange,
                                Expired = false
                            };

                            if (ContractFilter.ValidContract(newOption, OMCConfiguration))
                                OptionsManager.Persist(newOption);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}  - Critical Error @ProcessContractDetails: {1} ",
                                        OMCConfiguration.Name,
                                        ex.Message + " - " + ( ex.InnerException != null ? ex.InnerException.Message : "")),
                                        Main.Common.Util.Constants.MessageType.Error);
            }

        }

        protected void DoPublish()
        {
            while (true)
            {
                Thread.Sleep(OMCConfiguration.PublishUpdateInMilliseconds);

                if (IsMarketTime() && !(RunAdHocRecovery && !StillSecuritiesToDownload()))
                {
                    lock (tLock)
                    {
                        foreach (Security opt in ActiveOptions.Values)
                        {
                            Security underlying = ActiveSecurities.Values.Where(x => x.Symbol == opt.SymbolSfx).FirstOrDefault();

                            if (underlying != null && underlying.MarketData!=null)
                            {
                                opt.MarketData.CompositeUnderlyingPrice = underlying.MarketData.Trade;
                            }
                            else
                                DoLog(string.Format("@{0}  - Could not find underlying price for security: {1} ",
                                      OMCConfiguration.Name,opt.SymbolSfx ),Main.Common.Util.Constants.MessageType.Information);

                            RunPublishSecurity(opt);
                        }
                    }
                }

            }
        }

        #endregion

        #region Public Methods

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string moduleConfigFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;
                
                if (LoadConfig(moduleConfigFile))
                {
                    ActiveSecurities = new Dictionary<int, Security>();
                    SecuritiesToDownload = new Dictionary<string, bool>();
                    ContractsMarketData = new Dictionary<string, bool>();
                    ActiveOptions = new Dictionary<int, Security>();

                    if (!string.IsNullOrEmpty(OMCConfiguration.OptionsConverter))
                    {
                        var optionsConverter = Type.GetType(OMCConfiguration.OptionsConverter);
                        if (optionsConverter != null)
                        {
                            OptionConverter = (IOptionConverter)Activator.CreateInstance(optionsConverter);
                        }
                        else
                            throw new Exception("Assembly not found: " + OMCConfiguration.OptionsConverter);
                    }
                    else
                        throw new Exception("Option Converter assembly configuration not found.");

                    if (!string.IsNullOrEmpty(OMCConfiguration.ContractFilter))
                    {
                        var contractFilter = Type.GetType(OMCConfiguration.ContractFilter);
                        if (contractFilter != null)
                        {
                            ContractFilter = (IContractFilter)Activator.CreateInstance(contractFilter);
                        }
                        else
                            throw new Exception("Assembly not found: " + OMCConfiguration.ContractFilter);
                    }
                    else
                        throw new Exception(string.Format("Contract Filter assembly configuration not found:{0}", OMCConfiguration.ContractFilter));

                    ClientSocket = new EClientSocket(this);
                    
                    StockManager = new StockManager(OMCConfiguration.AccessLayerConnectionString);
                    OptionsManager = new OptionsManager(OMCConfiguration.AccessLayerConnectionString);
                    DailyOptionManager = new DailyOptionManager(OMCConfiguration.AccessLayerConnectionString);

                    PublishThread = new Thread(DoPublish);
                    PublishThread.Start();

                    ManualContractMarketDataDownloader = new Thread(DoManualContractMarketDataDownloader);
                    ManualContractMarketDataDownloader.Start();

                    ContractRealTimeMarketDataDownloader = new Thread(DoContractRealTimeMarketDataDownloader);
                    ContractRealTimeMarketDataDownloader.Start();

                    RunAdHocRecovery = false;


                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + moduleConfigFile, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critic error initializing {0}:{1}", moduleConfigFile,
                    ex.Message + "-" + (ex.InnerException != null ? ex.InnerException.Message : "")),
                                    Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    DoLog("Sending message " + action + " not implemented", Main.Common.Util.Constants.MessageType.Information);


                    return CMState.BuildFail(new Exception("Sending message " + action + " not implemented"));
                }
                else
                    throw new Exception("Invalid Wrapper");


            }
            catch (Exception ex)
            {
                DoLog(ex.Message, Main.Common.Util.Constants.MessageType.Error);
                throw;
            }
        }

        #endregion
    }
}