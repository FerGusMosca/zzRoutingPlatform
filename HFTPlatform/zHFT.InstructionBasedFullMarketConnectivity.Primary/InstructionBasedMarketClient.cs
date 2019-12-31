using QuickFix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.BasedFullMarketConnectivity.Primary.Common;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.InstructionBasedMarketClient.DataAccessLayer.Managers;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.BusinessEntities.Security_List;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.MarketClient.Primary.Common.Converters;
using zHFT.MarketClient.Primary.Common.Wrappers;
using zHFT.OrderRouters.Primary.Common;
using zHFT.OrderRouters.Primary.Common.Wrappers;
using zHFT.SingletonModulesHandler.Common.Interfaces;
using zHFT.SingletonModulesHandler.Common.Util;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.SecurityListSaver.BusinessEntities;
using zHFT.StrategyHandler.SecurityListSaver.Common.Interfaces;
using zHFT.StrategyHandler.SecurityListSaver.DataAccessLayer.Managers;

namespace zHFT.InstructionBasedFullMarketConnectivity.Primary
{
    public class InstructionBasedMarketClient : BaseFullMarketConnectivity, ISingletonModule
    {
        #region Constructors

        private InstructionBasedMarketClient(OnLogMessage pOnLogMsg, string configFile)
        {
            Initialize(pOnLogMsg, configFile);
        }

        #endregion

        #region Private Static Attributes

        private static InstructionBasedMarketClient Instance { get; set; }

        #endregion

        #region Public Static Methods

        public static InstructionBasedMarketClient GetInstance(OnLogMessage pOnLogMsg, string configFile)
        {
            if (Instance == null)
            {
                Instance = new InstructionBasedMarketClient(pOnLogMsg, configFile);
            }
            return Instance;
        }

        #endregion

        #region Protected Attributes

        protected IConfiguration Config { get; set; }

        protected ISecurityTranslator SecurityTranslator { get; set; }

        protected Common.Configuration.Configuration PrimaryConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        private InstructionManager InstructionManager { get; set; }

        private AccountManager AccountManager { get; set; }

        private PositionManager PositionManager { get; set; }

        protected MarketManager MarketManager { get; set; }

        protected StockManager StockManager { get; set; }

        protected FutureManager FutureManager { get; set; }

        protected BillManager BillManager { get; set; }

        protected OptionManager OptionManager { get; set; }

        protected StockMarkeDataManager StockMarketDataManager { get; set; }

        protected OptionMarketDataManager OptionMarketDataManager { get; set; }

        protected List<Market> Markets { get; set; }

        
        private Dictionary<int, Security> SecuritiesToPublish { get; set; }

        private Dictionary<int, DateTime> ContractsTimeStamps { get; set; }

        protected Thread MarketDataRequestThread { get; set; }

        protected Thread ProcessInstructionsThread { get; set; }

        protected Thread PublishThread { get; set; }

        protected Thread SaveMarketData { get; set; }

        protected Thread CleanOldSecuritiesThread { get; set; }

        protected OnMessageReceived OnMarketDataMessageRcv { get; set; }

        protected OnMessageReceived OnExecutionReportMessageRcv { get; set; }

        

        protected DateTime Start { get; set; }

        public DateTime? LastRoutingTimestamp { get; set; }

        protected object tLockRoutingFrequency { get; set; }

        #endregion

        #region Quickfix Objects Methods

        protected void ProcesssMDFullRefreshMessage(QuickFix.Message message)
        {

            string primarySymbol = message.getField(Symbol.FIELD);


            if (primarySymbol != null)
            {
                string market = ExchangeConverter.GetMarketFromPrimarySymbol(primarySymbol);
                string fullSymbol = SymbolConverter.GetFullSymbolFromPrimary(primarySymbol,market);
                zHFT.Main.Common.Enums.SecurityType secType;
                if (SecurityTypes.Keys.Contains(fullSymbol))
                    secType = SecurityTypes[fullSymbol];
                else
                {
                    DoLog(string.Format("@{0}:Could not find Security Type for symbol {1} ", PrimaryConfiguration.Name, primarySymbol), Main.Common.Util.Constants.MessageType.Error);
                    return;
                    //throw new Exception(string.Format("Could not find Security Type for symbol {0}", primarySymbol));
                }

                if(ActiveSecurities.Values.Any(x => x.Symbol == fullSymbol && x.Active)
                    && ExchangeConverter.IsValidClearingId(primarySymbol,secType))
                {
                    Security sec = ActiveSecurities.Values.Where(x => x.Symbol == fullSymbol && x.Active).FirstOrDefault();

                    if (sec != null)
                    {
                        DoLog(string.Format("@{0}:{1} ", PrimaryConfiguration.Name, message.ToString()), Main.Common.Util.Constants.MessageType.Information);

                        sec.MarketData.BestAskExch = market;
                        sec.MarketData.BestBidExch = market;
                        sec.MarketData.SettlType = ExchangeConverter.GetMarketSettlTypeID(secType, market);

                        FIXMessageCreator.ProcessMarketData(message, sec, OnLogMsg);

                        zHFT.MarketClient.Primary.Common.Wrappers.MarketDataWrapper mdWrapper = new zHFT.MarketClient.Primary.Common.Wrappers.MarketDataWrapper(sec, market, PrimaryConfiguration);

                        Thread publishMarketDataThread = new Thread(DoRunPublishSecurity);
                        publishMarketDataThread.Start(mdWrapper);

                    }
                    else
                    { 
                        //No dejamos nada registrdo para no sobrecargar la consola, pero aca estaríamos recibiendo market data
                        //de un security del que no nos des-suscribimos
                
                    }
                }
            }
            else
            {
                if (primarySymbol != null)
                    DoLog(string.Format("@{0}:Unknown market data for symbol {1} ", PrimaryConfiguration.Name, primarySymbol), Main.Common.Util.Constants.MessageType.Error);
                else
                    DoLog(string.Format("@{0}:Market data with no symbol", PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        #endregion

        #region Protected Methods

        #region SecurityList

        protected override void CancelMarketData(Security sec)
        {
            string market = ExchangeConverter.GetMarketFromPrimarySymbol(sec.Symbol);
            string fullSymbol = SymbolConverter.GetFullSymbolFromPrimary(sec.Symbol, market);

            if (ActiveSecurities.Values.Any(x => x.Symbol == fullSymbol))
            {
                List<int> toRemove = new List<int>();
                foreach (int key in ActiveSecurities.Keys)
                {
                    Security kSec = ActiveSecurities[key];

                    if (kSec.Symbol == fullSymbol)
                        kSec.Active = false;

                }

                DoLog(string.Format("@{0}:Unsubscribing Market Data On Demand for Symbol: {0}", GetConfig().Name, sec.Symbol), Main.Common.Util.Constants.MessageType.Information);
            }
            else
                throw new Exception(string.Format("@{0}: Could not find active security to unsubscribe for symbol {1}", GetConfig().Name, fullSymbol));
           
        }

        protected void ProcessStocksList(List<Security> stocksSecurities)
        {

            foreach (Market market in Markets)
            {
                foreach (Security security in stocksSecurities.Where(x => x.Exchange == market.Code))
                {
                    zHFT.StrategyHandler.SecurityListSaver.BusinessEntities.Stock stock = new zHFT.StrategyHandler.SecurityListSaver.BusinessEntities.Stock();
                    stock.Market = market;
                    stock.Country = market.Country;
                    stock.Symbol = security.Symbol.Trim();
                    stock.Name = "";
                    stock.Category = "";

                    zHFT.StrategyHandler.SecurityListSaver.BusinessEntities.Stock prevStock = StockManager.GetByCode(security.Symbol.Trim(), market.Code, market.Country);

                    if (prevStock == null)
                    {
                        if (PrimaryConfiguration.RequestSecurityList)
                        {
                            try
                            {
                                stock.LoadFinalSymbol();

                                StockManager.Persist(stock);
                                DoLog(string.Format("Inserting new stock from market {0}:", stock.Symbol),
                                      Main.Common.Util.Constants.MessageType.Information);
                            }
                            catch (Exception ex)
                            {
                                DoLog(string.Format("Error saving new stock for symbol {0}:{1}", stock.Symbol, ex.Message),
                                                    Main.Common.Util.Constants.MessageType.Error);
                            }
                        }
                        else
                        {
                            DoLog(string.Format("@{0}: New symbol {0} not saved because of configuration", stock.Symbol), Main.Common.Util.Constants.MessageType.Information);
                        }
                    }
                    else
                    {
                        DoLog(string.Format("Stock {0} already existed", stock.Symbol), Main.Common.Util.Constants.MessageType.Information);
                    }
                }
            }
        }

        protected void TraceUnderlying(Security option)
        {
            Option temp = new Option();
            temp.Exchange = option.Exchange;
            temp.Symbol = option.Symbol;

            string symbolSfxPrefix = temp.GetSymboSfxPrefix();

            Option optExample = OptionManager.GetOptionByPrefix(symbolSfxPrefix, option.Exchange);

            if (optExample != null)
            {
                option.SymbolSfx = optExample.SymbolSfx;
                option.PutOrCall = temp.GetPutOrCall();
                option.StrikeCurrency = temp.GetStrikeCurrency();
            }
            else
                throw new Exception(string.Format("Could not find underlying for symbol {0}", option.Symbol));
        }

        protected void ProcessOptionsList(List<Security> optionSecurities)
        {
            foreach (Market market in Markets)
            {
                foreach (Security option in optionSecurities.Where(x => x.Exchange == market.Code))
                {
                    Option prevOption = OptionManager.GetBySymbol(option.Symbol.Trim(), market.Code);

                    if (prevOption == null)
                    {
                        if (PrimaryConfiguration.RequestSecurityList)
                        {
                            try
                            {
                                TraceUnderlying(option);//We need to know the SymbolSfx field using other options
                                OptionManager.Insert(option);
                                DoLog(string.Format("Inserting new option contract from market: {0}", option.Symbol),
                                      Main.Common.Util.Constants.MessageType.Information);
                            }
                            catch (Exception ex)
                            {
                                DoLog(string.Format("Error trying to save option for symbol {0} : {1}", option.Symbol, ex.Message),
                                    Main.Common.Util.Constants.MessageType.Error);

                            }
                        }
                        else
                        {
                            DoLog(string.Format("@{0}: New symbol {0} not saved because of configuration", option.Symbol), Main.Common.Util.Constants.MessageType.Information);
                        }

                    }
                    else
                    {
                        DoLog(string.Format("Option {0} already existed", prevOption.Symbol), Main.Common.Util.Constants.MessageType.Information);
                    }
                }
            }
        }

        protected void ProcessBillsList(List<Security> billsSecurities)
        {
            foreach (Market market in Markets)
            {
                foreach (Security bill in billsSecurities.Where(x => x.Exchange == market.Code))
                {
                    Bill prevBill = BillManager.GetBySymbol(bill.Symbol.Trim(), market.Code);

                    if (prevBill == null)
                    {
                        if (PrimaryConfiguration.RequestSecurityList)
                        {
                            try
                            {
                                BillManager.Insert(bill);
                                DoLog(string.Format("Inserting new bill from market: {0}", bill.Symbol),
                                      Main.Common.Util.Constants.MessageType.Information);
                            }
                            catch (Exception ex)
                            {
                                DoLog(string.Format("Error trying to save bill for symbol {0} : {1}", bill.Symbol, ex.Message),
                                    Main.Common.Util.Constants.MessageType.Error);

                            }
                        }
                        else
                        {
                            DoLog(string.Format("@{0}: New symbol {0} not saved because of configuration", bill.Symbol), Main.Common.Util.Constants.MessageType.Information);
                        }

                    }
                    else
                    {
                        DoLog(string.Format("Bill {0} already existed", prevBill.Symbol), Main.Common.Util.Constants.MessageType.Information);
                    }
                }
            }
        
        }

        protected void ProcessFutureList(List<Security> futureSecurities)
        {
            foreach (Market market in Markets)
            {
                foreach (Security future in futureSecurities.Where(x => x.Exchange == market.Code))
                {
                    Future prevFuture = FutureManager.GetBySymbol(future.Symbol.Trim(), market.Code);

                    if (prevFuture == null)
                    {
                        if (PrimaryConfiguration.RequestSecurityList)
                        {
                            try
                            {
                                FutureManager.Insert(future);
                                DoLog(string.Format("Inserting new future contract from market: {0}", future.Symbol),
                                      Main.Common.Util.Constants.MessageType.Information);
                            }
                            catch (Exception ex)
                            {
                                DoLog(string.Format("Error trying to save future for symbol {0} : {1}", future.Symbol, ex.Message),
                                    Main.Common.Util.Constants.MessageType.Error);

                            }
                        }
                        else
                        {
                            DoLog(string.Format("@{0}: New symbol {0} not saved because of configuration", future.Symbol), Main.Common.Util.Constants.MessageType.Information);
                        }

                    }
                    else
                    {
                        DoLog(string.Format("Future {0} already existed", prevFuture.Symbol), Main.Common.Util.Constants.MessageType.Information);
                    }
                }
            }
        }

        protected override void ProcessSecurities(SecurityList securityList)
        {
            SecurityTranslator.DoTranslate(securityList);

            foreach (string secType in PrimaryConfiguration.SecurityTypes)
            {
                if (secType == zHFT.Main.Common.Enums.SecurityType.CS.ToString())
                {
                    List<Security> stocksSecurities = securityList.Securities.Where(x => x.SecType == zHFT.Main.Common.Enums.SecurityType.CS).ToList();
                    ProcessStocksList(stocksSecurities);
                }
                else if (secType == zHFT.Main.Common.Enums.SecurityType.OPT.ToString())
                {
                    List<Security> optionSecurities = securityList.Securities.Where(x => x.SecType == zHFT.Main.Common.Enums.SecurityType.OPT).ToList();
                    ProcessOptionsList(optionSecurities);
                }
                else if (secType == zHFT.Main.Common.Enums.SecurityType.FUT.ToString())
                {
                    List<Security> futureSecurities = securityList.Securities.Where(x => x.SecType == zHFT.Main.Common.Enums.SecurityType.FUT).ToList();
                    ProcessFutureList(futureSecurities);
                }
                else if (secType == zHFT.Main.Common.Enums.SecurityType.TB.ToString())
                {
                    List<Security> billsSecurities = securityList.Securities.Where(x => x.SecType == zHFT.Main.Common.Enums.SecurityType.TB).ToList();
                    ProcessBillsList(billsSecurities);
                }
                else
                {
                    DoLog(string.Format("@{0}: Security Type not handled {1}:", PrimaryConfiguration.Name, secType),
                        Main.Common.Util.Constants.MessageType.Error);
                }
            }
        }

        #endregion

        #region Instructions

        protected void ProcessPositionInstruction(Instruction instr)
        {
            try
            {
                if (instr != null)
                {
                    if (!ActiveSecurities.Values.Any(x => x.Symbol == instr.Symbol))
                    {
                        instr = InstructionManager.GetById(instr.Id);

                        if (instr.InstructionType.Type == InstructionType._NEW_POSITION || instr.InstructionType.Type == InstructionType._UNWIND_POSITION)
                        {
                            Security sec = new Security()
                            {
                                Symbol = instr.Symbol,
                                Exchange = ExchangeConverter.GetMarketFromFullSymbol(instr.Symbol),
                                SecType = instr.SecurityType
                            };

                            
                            ContractsTimeStamps.Add(instr.Id, DateTime.Now);
                            SecuritiesToPublish.Add(instr.Id, sec);
                            if(!SecurityTypes.Keys.Contains(instr.Symbol))
                                SecurityTypes.Add(instr.Symbol, instr.SecurityType);

                            if (!PrimaryConfiguration.RequestFullMarketData)//No tenemos todos los securities
                            {
                                lock (tLock)
                                {
                                    ActiveSecurities.Add(instr.Id, sec);
                                }
                                MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(sec,zHFT.Main.Common.Enums.SubscriptionRequestType.SnapshotAndUpdates);
                                ProcessMarketDataRequest(wrapper);

                            }
                        }
                    }
                }
                else
                    throw new Exception(string.Format("@{1}Could not find a related instruction for id {0}", instr.Id, PrimaryConfiguration.Name));
            }
            catch (Exception ex)
            {

                DoLog(string.Format("@{2}:Critical error processing related instruction: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""), PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected void DoFindInstructions()
        {
            while (true)
            {
                Thread.Sleep(PrimaryConfiguration.SearchForInstructionsInMilliseconds);

                List<Instruction> instructionsToProcess = InstructionManager.GetPendingInstructions(PrimaryConfiguration.AccountNumber);

                try
                {
                    foreach (Instruction instr in instructionsToProcess.Where(x => x.InstructionType.Type == InstructionType._NEW_POSITION || x.InstructionType.Type == InstructionType._UNWIND_POSITION))
                    {
                        //We process the account positions sync instructions
                        ProcessPositionInstruction(instr);
                    }
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{2}:Critical error processing instructions: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""), PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                }
            }
        }

        protected void DoCleanOldSecurities()
        {
            while (true)
            {
                Thread.Sleep(_SECURITIES_REMOVEL_PERIOD);//Once every hour

                try
                {
                    List<int> keysToRemove = new List<int>();
                    foreach (int key in ContractsTimeStamps.Keys)
                    {
                        DateTime timeStamp = ContractsTimeStamps[key];

                        if ((DateTime.Now - timeStamp).Hours >= _MAX_ELAPSED_HOURS_FOR_MARKET_DATA)
                        {
                            keysToRemove.Add(key);
                        }
                    }

                    foreach (int keyToRemove in keysToRemove)
                    {
                        ContractsTimeStamps.Remove(keyToRemove);
                        if (PrimaryConfiguration.RequestFullMarketData)
                            SecuritiesToPublish.Remove(keyToRemove);//Solo sacamos los securities que se publican
                        else
                        {
                            Security security = ActiveSecurities[keyToRemove];

                            lock (tLock)
                            {
                                ActiveSecurities.Remove(keyToRemove);//los que se publican y aquellos de los que se tiene md son lo mismo
                            }
                            
                            MarketDataRequestWrapper mdr = new MarketDataRequestWrapper(security, Main.Common.Enums.SubscriptionRequestType.Unsuscribe);
                            ProcessMarketDataRequest(mdr);
                        }

                    }
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{1}:There was an error cleaning old securities from market data flow error={0} ",
                            ex.Message, PrimaryConfiguration.Name),
                            Main.Common.Util.Constants.MessageType.Error);
                }
                
            }
        }

        #endregion

        #region Market Data

        protected void DoRunPublishSecurity(object param)
        {
            zHFT.MarketClient.Primary.Common.Wrappers.MarketDataWrapper wrapper = (zHFT.MarketClient.Primary.Common.Wrappers.MarketDataWrapper)param;

            try
            {
                OnMarketDataMessageRcv(wrapper);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error on Publishing Market Data Thread. Error={1} ",PrimaryConfiguration.Name, ex.Message),
                      Main.Common.Util.Constants.MessageType.Error);
            
            }
        
        }

        protected void RunPublishSecurity(Security sec, IConfiguration Config)
        {
            try
            {
                zHFT.MarketClient.Primary.Common.Wrappers.MarketDataWrapper wrapper = new zHFT.MarketClient.Primary.Common.Wrappers.MarketDataWrapper(sec, sec.Exchange, Config);

                Thread publishMarketDataThread = new Thread(DoRunPublishSecurity);
                publishMarketDataThread.Start(wrapper);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{2}:Error Publishing Market Data for Security {0}. Error={1} ",
                                            sec.Symbol, ex != null ? ex.Message : "",
                                            PrimaryConfiguration.Name),
                                            Main.Common.Util.Constants.MessageType.Error);
            }

        }

        protected void RequestBulkMarketData(string secType)
        {
            if (PrimaryConfiguration.SecurityTypes == null)
                return;

            if (secType == zHFT.Main.Common.Enums.SecurityType.CS.ToString())
            {
                foreach (Market market in Markets)
                {
                    IList<zHFT.StrategyHandler.SecurityListSaver.BusinessEntities.Stock> stocks = StockManager.GetByMarket(market.Code);

                    foreach (zHFT.StrategyHandler.SecurityListSaver.BusinessEntities.Stock stock in stocks)
                    {
                        Security stockSecToRequest = new Security();
                        stockSecToRequest.Symbol = stock.Symbol;
                        stockSecToRequest.Exchange = market.Code;
                        stockSecToRequest.SecType = zHFT.Main.Common.Enums.SecurityType.CS;

                        lock (tLock)
                        {
                            ActiveSecurities.Add(ActiveSecurities.Count() + 1, stockSecToRequest);
                            SecurityTypes.Add(stock.Symbol, zHFT.Main.Common.Enums.SecurityType.CS);
                        }
                        MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(stockSecToRequest,zHFT.Main.Common.Enums.SubscriptionRequestType.SnapshotAndUpdates);
                        ProcessMarketDataRequest(wrapper);
                    }
                }

            }
            else if (secType == zHFT.Main.Common.Enums.SecurityType.OPT.ToString())
            {

                foreach (Market market in Markets)
                {
                    IList<Option> options = OptionManager.GetByMarket(market.Code);

                    foreach (Option option in options)
                    {
                        Security optSecToRequest = new Security();
                        optSecToRequest.Symbol = option.Symbol;
                        optSecToRequest.Exchange = market.Code;
                        optSecToRequest.SecType = zHFT.Main.Common.Enums.SecurityType.OPT;

                        lock (tLock)
                        {
                            ActiveSecurities.Add(ActiveSecurities.Count() + 1, optSecToRequest);
                            SecurityTypes.Add(option.Symbol, zHFT.Main.Common.Enums.SecurityType.OPT);
                        }
                        MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(optSecToRequest,zHFT.Main.Common.Enums.SubscriptionRequestType.SnapshotAndUpdates);
                        ProcessMarketDataRequest(wrapper);
                    }
                }
            }
            else
            {
                DoLog(string.Format("@{0}: Could not handle market data for asset class {1}:", PrimaryConfiguration.Name, secType),
                                    Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected void RunSaveMarketData(Object param)
        {
            Security sec = (Security)param;

            if (sec == null || sec.MarketData == null || sec.MarketData.MDEntryDate == null)
                return;
            try
            {
                DoLog(string.Format("@{1}:Saving Market Data For Symbol={0} ", sec.Symbol, PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);

                if (sec.SecType == zHFT.Main.Common.Enums.SecurityType.CS)
                {
                    StockMarketDataManager.Persist(sec);
                }
                else if (sec.SecType == zHFT.Main.Common.Enums.SecurityType.OPT)
                {
                    Option opt = OptionManager.GetBySymbol(sec.Symbol, sec.Exchange);

                    opt.MarketData = sec.MarketData;

                    OptionMarketDataManager.Persist(opt);
                }
                else
                    DoLog(string.Format("Market Data not implemented for security type {0} in symbol {1}", sec.SecType.ToString(), sec.Symbol), Main.Common.Util.Constants.MessageType.Error);
               

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{2}:Error Saving Market Data for Security {0}. Error={1} ",
                                            sec.Symbol, ex != null ? ex.Message : "",
                                            PrimaryConfiguration.Name),
                                            Main.Common.Util.Constants.MessageType.Error);
            }


        }

        protected void DoPublishMarketData()
        {
            while (true)
            {
                Thread.Sleep(PrimaryConfiguration.PublishUpdateInMilliseconds);

                try
                {

                    lock (tLock)
                    {
                        foreach (Security sec in ActiveSecurities.Values.Where(x => x.Active))
                        {
                            if (SecuritiesToPublish.Values.Any(x => x.Symbol == sec.Symbol))
                                RunPublishSecurity(sec, PrimaryConfiguration);

                            if (PrimaryConfiguration.SaveFullMarketData)
                            {
                                SaveMarketData = new Thread(RunSaveMarketData);
                                SaveMarketData.Start(sec);
                            }
                        }

                        //Removemos los que no están mas activos
                        List<int> toRemove = new List<int>();
                        foreach (int key in ActiveSecurities.Keys)
                        {
                            if (!ActiveSecurities[key].Active)
                                toRemove.Add(key);
                        }

                        toRemove.ForEach(x => ActiveSecurities.Remove(x));

                    }
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{0}:Critic error publishing market data :{1}", PrimaryConfiguration.Name,
                                             ex.Message), Main.Common.Util.Constants.MessageType.Error);
                }
            }
        }

        protected void DoRequestMarketData()
        {
            bool active = PrimaryConfiguration.RequestFullMarketData;

            while (active)
            {
                Thread.Sleep(PrimaryConfiguration.MaxWaitingTimeForMarketDataRequest * 1000);

                try
                {

                    TimeSpan elapsed = DateTime.Now - Start;

                    if (elapsed.TotalSeconds > Convert.ToDouble(PrimaryConfiguration.MaxWaitingTimeForMarketDataRequest))
                    {
                        foreach (string secType in PrimaryConfiguration.SecurityTypes)
                            RequestBulkMarketData(secType);
                        
                        active = false;
                    }
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{0}:Critic error requessting market data :{1}", PrimaryConfiguration.Name,
                                         ex.Message), Main.Common.Util.Constants.MessageType.Error);
                    active = false;
                }
            }
        }

        protected override void DoRunNewOrder(object param)
        {
            QuickFix.Message nosMessage = (QuickFix.Message)param;

            try
            {

                lock (tLockRoutingFrequency)
                {

                    if (PrimaryConfiguration.WaitingTimeForOrderRoutingInMiliseconds.HasValue)
                    {

                        while((DateTime.Now - LastRoutingTimestamp.Value).TotalMilliseconds<PrimaryConfiguration.WaitingTimeForOrderRoutingInMiliseconds.Value)
                        {

                            Thread.Sleep(PrimaryConfiguration.WaitingTimeForOrderRoutingInMiliseconds.Value);
                        }
                    }


                    DoLog(string.Format("@{0}:Sending New Order Message Thread: {1}! ", GetConfig().Name, nosMessage.ToString()), Main.Common.Util.Constants.MessageType.Information);
                    Session.sendToTarget(nosMessage, SessionID);
                    LastRoutingTimestamp = DateTime.Now;
                    DoLog(string.Format("@{0}:New Order Message Thread: Message succesfully sent: {1}! ", GetConfig().Name, nosMessage.ToString()), Main.Common.Util.Constants.MessageType.Information);

                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error sending new order message {1}: {2}! ", GetConfig().Name, nosMessage.ToString(), ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        #endregion

        #endregion

        #region Public Methods

        public  CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {

                    if (wrapper.GetAction() == Actions.NEW_ORDER)
                    {
                        DoLog(string.Format("@{0}:Routing with Primary to market for symbol {1}", PrimaryConfiguration.Name, wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        return RouteNewOrder(wrapper);
                    }
                    else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                    {
                        DoLog(string.Format("@{0}:Updating order with Primary  for symbol {1}", PrimaryConfiguration.Name, wrapper.GetField(OrderFields.ClOrdID).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        UpdateOrder(wrapper);
                        return CMState.BuildSuccess();

                    }
                    else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                    {
                        DoLog(string.Format("@{0}:Canceling order with Primary  for ClOrdId {1}", PrimaryConfiguration.Name, wrapper.GetField(OrderFields.ClOrdID).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        CancelOrder(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else if (wrapper.GetAction() == Actions.MARKET_DATA_REQUEST)
                    {
                        DoLog(string.Format("Receiving Market Data Request: {0}", wrapper.ToString()), Main.Common.Util.Constants.MessageType.Information);
                        return ProcessMarketDataRequest(wrapper);
                    }
                    else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
                    {
                        DoLog(string.Format("@{0}:Cancelling all active orders @ Primary", PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                        return CancelAllOrders();
                    }
                    else
                    {

                        Actions action = wrapper.GetAction();
                        DoLog(string.Format("@{0}:Sending message " + action + " not implemented", PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception(string.Format("@{0}:Sending message " + action + " not implemented", PrimaryConfiguration.Name)));
                    }
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

        public  bool Initialize(OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnLogMsg += pOnLogMsg;

                if (zHFT.SingletonModulesHandler.Common.Util.ConfigLoader.DoLoadConfig(this,configFile))
                {
                    ActiveSecurities = new Dictionary<int, Security>();
                    ContractsTimeStamps = new Dictionary<int, DateTime>();
                    OrderConverter = new OrderConverter();
                    SecurityListConverter = new SecurityListConverter();
                    ActiveOrders = new Dictionary<string, Order>();
                    ActiveOrderIdMapper = new Dictionary<string, int>();
                    ReplacingActiveOrderIdMapper = new Dictionary<string, int>();
                    SecuritiesToPublish = new Dictionary<int,Security>();
                    OrderIndexId = GetNextOrderId();
                    Start = DateTime.Now;

                    InstructionManager = new InstructionManager(PrimaryConfiguration.InstructionsAccessLayerConnectionString);
                    PositionManager = new PositionManager(PrimaryConfiguration.InstructionsAccessLayerConnectionString);
                    AccountManager = new AccountManager(PrimaryConfiguration.InstructionsAccessLayerConnectionString);
                    MarketManager = new MarketManager(PrimaryConfiguration.SecuritiesAccessLayerConnectionString);
                    StockManager = new StockManager(PrimaryConfiguration.SecuritiesAccessLayerConnectionString);
                    OptionManager = new OptionManager(PrimaryConfiguration.SecuritiesAccessLayerConnectionString);
                    FutureManager = new FutureManager(PrimaryConfiguration.SecuritiesAccessLayerConnectionString);
                    BillManager = new BillManager(PrimaryConfiguration.SecuritiesAccessLayerConnectionString);
                    StockMarketDataManager = new StockMarkeDataManager(PrimaryConfiguration.SecuritiesAccessLayerConnectionString);
                    OptionMarketDataManager = new OptionMarketDataManager(PrimaryConfiguration.SecuritiesAccessLayerConnectionString);


                    var fixMessageCreator = Type.GetType(PrimaryConfiguration.FIXMessageCreator);
                    if (fixMessageCreator != null)
                    {
                        FIXMessageCreator = (IFIXMessageCreator)Activator.CreateInstance(fixMessageCreator);
                    }
                    else
                        throw new Exception(string.Format("@{0}:Assembly not found: " + PrimaryConfiguration.FIXMessageCreator));

                    var typeMarketTranslator = Type.GetType(PrimaryConfiguration.SecuritiesMarketTranslator);
                    if (typeMarketTranslator != null)
                        SecurityTranslator = (ISecurityTranslator)Activator.CreateInstance(typeMarketTranslator);
                    else
                    {
                        DoLog("assembly not found: " + PrimaryConfiguration.SecuritiesMarketTranslator, Main.Common.Util.Constants.MessageType.Error);
                        return false;
                    }

                    Markets = new List<Market>();
                    foreach (string market in PrimaryConfiguration.Markets)
                    {
                        Market mrk = MarketManager.GetByCode(market);
                        if (mrk != null)
                            Markets.Add(mrk);
                        else
                        {
                            DoLog(string.Format("@{0}:Market {0} not found", market), Main.Common.Util.Constants.MessageType.Error);
                        }
                    }

                    SecurityTypes = new Dictionary<string, Main.Common.Enums.SecurityType>();

                    SessionSettings = new SessionSettings(PrimaryConfiguration.FIXInitiatorPath);
                    FileStoreFactory = new FileStoreFactory(SessionSettings);
                    ScreenLogFactory = new ScreenLogFactory(SessionSettings);
                    MessageFactory = new DefaultMessageFactory();

                    Initiator = new SocketInitiator(this, FileStoreFactory, SessionSettings, ScreenLogFactory, MessageFactory);

                    Initiator.start();

                    PublishThread = new Thread(DoPublishMarketData);
                    PublishThread.Start();

                    CleanOldSecuritiesThread = new Thread(DoCleanOldSecurities);
                    CleanOldSecuritiesThread.Start();

                    ProcessInstructionsThread = new Thread(DoFindInstructions);
                    ProcessInstructionsThread.Start();

                    MarketDataRequestThread = new Thread(DoRequestMarketData);
                    MarketDataRequestThread.Start();

                    tLockRoutingFrequency = new object();

                    if (PrimaryConfiguration.WaitingTimeForOrderRoutingInMiliseconds.HasValue)
                        LastRoutingTimestamp = DateTime.Now;

                    return true;

                }
                else
                {
                    DoLog(string.Format("@{0}:Error initializing config file " + configFile, PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Critic error initializing {1}:{2}", PrimaryConfiguration.Name,
                                                                              configFile,
                                                                              ex.Message),
                                                                              Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        public override BaseConfiguration GetConfig() { return (BaseConfiguration)PrimaryConfiguration; } 

        #endregion

        #region QuickFix Methods

        public override void fromApp(Message value, SessionID sessionId)
        {
            try
            {
                DoLog("Invocación de fromApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);

                if (value is QuickFix50.MarketDataIncrementalRefresh)
                {
                    DoLog(string.Format("{0}: Market Data Incremental Refresh Message received and not processed:{1} ", PrimaryConfiguration.Name, value.ToString()), Constants.MessageType.Error);
                }
                else if (value is QuickFix50.MarketDataSnapshotFullRefresh)
                {
                    QuickFix50.MarketDataSnapshotFullRefresh msg = (QuickFix50.MarketDataSnapshotFullRefresh)value;
                    ProcesssMDFullRefreshMessage(msg);
                }
                else if (value is QuickFix50.SecurityList)
                {
                    SecurityListWrapper wrapper = new SecurityListWrapper((QuickFix50.SecurityList)value, (IConfiguration)Config);
                    ProcessSecurityList(wrapper);
                }
                else if (value is QuickFix50.ExecutionReport)
                {
                    QuickFix50.ExecutionReport msg = (QuickFix50.ExecutionReport)value;
                    ExecutionReportWrapper erWrapper = ProcesssExecutionReportMessage(msg);
                    OnExecutionReportMessageRcv(erWrapper);
                }
                else if (value is QuickFix50Sp2.MarketDataRequestReject)
                {
                    DoLog(string.Format("{0}: MarketDataRequestReject:{1} ", PrimaryConfiguration.Name, value.ToString()), Constants.MessageType.Error);
                }
                else
                {
                    DoLog(string.Format("{0}: Unknown message:{1} ", PrimaryConfiguration.Name, value.ToString()), Constants.MessageType.Information);
                }
            }
            catch (Exception ex)
            {

                DoLog(string.Format("{0}: Error processing message @fromApp:{1} ", PrimaryConfiguration.Name, ex.Message), Constants.MessageType.Error);

            }
            
        }

        public override void toAdmin(Message value, SessionID sessionId)
        {
            try
            {
                if (value is QuickFixT11.Logon)
                {
                    QuickFixT11.Logon logon = (QuickFixT11.Logon)value;
                    logon.setField(Username.FIELD, PrimaryConfiguration.User);
                    logon.setField(Password.FIELD, PrimaryConfiguration.Password);
                    DoLog("Invocación de toAdmin-logon por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
                }
                else if (value is QuickFixT11.Reject)
                {
                    QuickFixT11.Reject reject = (QuickFixT11.Reject)value;
                    DoLog("Invocación de toAdmin-reject por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
                }
                else
                    DoLog("Invocación de toAdmin por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("{0}: Error processing message @toAdmin:{1} ", PrimaryConfiguration.Name, ex.Message), Constants.MessageType.Error);
            }
            
        }

        public override void onLogon(SessionID value)
        {
            SessionID = value;
            DoLog("Invocación de onLogon : " + value.ToString(), Constants.MessageType.Information);

            if (SessionID != null)
            {
                if (PrimaryConfiguration.RequestSecurityList)
                {
                    SecurityListRequestWrapper slWrapper = new SecurityListRequestWrapper(zHFT.Main.Common.Enums.SecurityListRequestType.AllSecurities, null);
                    ProcessSecurityListRequest(slWrapper);
                }
                DoLog(string.Format("Logged for SessionId : {0}", value.ToString()), Constants.MessageType.Information);
                    
            }
            else
                DoLog("Error logging to FIX Session! : " + value.ToString(), Constants.MessageType.Error);
        }

        #endregion

        #region ISingletonModule Methods

        public void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        public void SetOutgoingEvent(OnMessageReceived OnMessageRcv)
        {
            OnExecutionReportMessageRcv += OnMessageRcv;
        }

        public void SetIncomingEvent(OnMessageReceived OnMessageRcv)
        {
            OnMarketDataMessageRcv += OnMessageRcv;
        }

        #endregion
    }
}
