using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.BusinessEntities.Security_List;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.SecurityListSaver.BusinessEntities;
using zHFT.StrategyHandler.SecurityListSaver.Common.Interfaces;
using zHFT.StrategyHandler.SecurityListSaver.DataAccessLayer.Managers;

namespace zHFT.StrategyHandler.SecurityListSaver
{
    public class SecurityListSaver : BaseCommunicationModule
    {

        #region Protected Attributes

        protected SecurityListConverter SecurityListConverter { get; set; }

        protected zHFT.StrategyHandler.SecurityListSaver.Common.Configuration.Configuration SecurityListSaverConfiguration
        {
            get { return (zHFT.StrategyHandler.SecurityListSaver.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected ISecurityTranslator SecurityTranslator { get; set; }

        protected MarketManager MarketManager { get; set; }

        protected StockManager StockManager { get; set; }

        protected OptionManager OptionManager { get; set; }

        protected CountryManager CountryManager { get; set; }

        protected StockMarkeDataManager StockMarketDataManager { get; set; }

        protected OptionMarketDataManager OptionMarketDataManager { get; set; }

        protected List<Market> Markets { get; set; }

        protected Country MainCountry { get; set; }

        protected Dictionary<string, bool> MarketDataRequested { get; set; }

        protected Thread MarketDataRequestThread { get; set; }

        protected DateTime Start { get; set; }

        #endregion

        #region ProtectedMethods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new zHFT.StrategyHandler.SecurityListSaver.Common.Configuration.Configuration().GetConfiguration<zHFT.StrategyHandler.SecurityListSaver.Common.Configuration.Configuration>(configFile, noValueFields);
        }

        #endregion

        #region Private Methods

        protected void RequestMarketData(string secType)
        {
            if (SecurityListSaverConfiguration.SecurityTypes == null)
                return;
          
            if (secType == SecurityType.CS.ToString())
            {
                foreach (Market market in Markets)
                {
                    IList<Stock> stocks = StockManager.GetByMarket(market.Code);

                    foreach(Stock stock in stocks)
                    {
                        Security stockSecToRequest = new Security();
                        stockSecToRequest.Symbol = stock.Symbol;
                        stockSecToRequest.Exchange = market.Code;
                        stockSecToRequest.SecType = SecurityType.CS;

                        MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(stockSecToRequest, SubscriptionRequestType.SnapshotAndUpdates);
                        OnMessageRcv(wrapper);
                    }
                }
                
            }
            else if (secType == SecurityType.OPT.ToString())
            {

                foreach (Market market in Markets)
                {
                    IList<Option> options = OptionManager.GetByMarket(market.Code);

                    foreach (Option option in options)
                    {
                        Security stockSecToRequest = new Security();
                        stockSecToRequest.Symbol = option.Symbol;
                        stockSecToRequest.Exchange = market.Code;
                        stockSecToRequest.SecType = SecurityType.OPT;

                        MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(stockSecToRequest, SubscriptionRequestType.SnapshotAndUpdates);
                        OnMessageRcv(wrapper);
                    }
                }
            }
            else
            {
                DoLog(string.Format("@{0}: Could not handle market data for asset class {1}:", SecurityListSaverConfiguration.Name, secType),
                                    Main.Common.Util.Constants.MessageType.Error);
            }
           
        }

        protected void ProcessStocksList(List<Security> stocksSecurities)
        {
           
            foreach (Market market in Markets)
            {
                foreach (Security security in stocksSecurities.Where(x => x.Exchange == market.Code))
                {
                    Stock stock = new Stock();
                    stock.Market = market;
                    stock.Country = MainCountry.Code;
                    stock.Symbol = security.Symbol.Trim();
                    stock.Name = "";
                    stock.Category = "";

                    Stock prevStock = StockManager.GetByCode(security.Symbol.Trim(), market.Code, MainCountry.Code);

                    if (prevStock == null)
                    {
                        if (SecurityListSaverConfiguration.SaveNewSecurities.HasValue && SecurityListSaverConfiguration.SaveNewSecurities.Value)
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
                                DoLog(string.Format("Error saving new stock for symbol {0}:{1}", stock.Symbol,ex.Message),
                                                    Main.Common.Util.Constants.MessageType.Error);

                            
                            }
                        }
                        else
                        {
                            DoLog(string.Format("@{0}: New symbol {0} not saved because of configuration", stock.Symbol),
                                      Main.Common.Util.Constants.MessageType.Information);

                        }

                    }
                    else
                    {
                        DoLog(string.Format("Stock {0} already existed", stock.Symbol),
                                 Main.Common.Util.Constants.MessageType.Information);
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
                        if (SecurityListSaverConfiguration.SaveNewSecurities.HasValue && SecurityListSaverConfiguration.SaveNewSecurities.Value)
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
                                DoLog(string.Format("Error trying to save option for symbol {0} : {1}", option.Symbol,ex.Message),
                                    Main.Common.Util.Constants.MessageType.Error);
                            
                            }
                        }
                        else
                        {
                            DoLog(string.Format("@{0}: New symbol {0} not saved because of configuration", option.Symbol),
                                      Main.Common.Util.Constants.MessageType.Information);

                        }

                    }
                    else
                    {
                        DoLog(string.Format("Stock {0} already existed", prevOption.Symbol),
                                 Main.Common.Util.Constants.MessageType.Information);
                    }
                }
            }
        }

        protected void ProcessSecurities(SecurityList securityList)
        {
            SecurityTranslator.DoTranslate(securityList);

            foreach (string secType in SecurityListSaverConfiguration.SecurityTypes)
            {
                if (secType == SecurityType.CS.ToString())
                {
                    List<Security> stocksSecurities = securityList.Securities.Where(x => x.SecType == SecurityType.CS).ToList();
                    ProcessStocksList(stocksSecurities);
                }
                else if (secType == SecurityType.OPT.ToString())
                {
                    List<Security> optionSecurities = securityList.Securities.Where(x => x.SecType == SecurityType.OPT).ToList();
                    ProcessOptionsList(optionSecurities);
                }
                else
                {
                    DoLog(string.Format("@{0}: Security Type not handled {1}:", SecurityListSaverConfiguration.Name, secType),
                        Main.Common.Util.Constants.MessageType.Error);
                }
            }
        }

        protected CMState ProcessMarketData(Wrapper wrapper)
        {
            try
            {
            
                MarketDataConverter conv = new MarketDataConverter();

                MarketData md = conv.GetMarketData(wrapper, Config);

                Security sec = md.Security;
                sec.MarketData = md;

                if (sec.SecType == SecurityType.CS)
                {

                    StockMarketDataManager.Persist(sec);

                    return CMState.BuildSuccess();
                }
                else if (sec.SecType == SecurityType.OPT)
                {
                    Option opt = OptionManager.GetBySymbol(sec.Symbol, sec.Exchange);

                    opt.MarketData = md;

                    OptionMarketDataManager.Persist(opt);

                    return CMState.BuildSuccess();

                }
                else
                    throw new Exception(string.Format("Market Data not implemented for security type {0} in symbol {1}", sec.SecType.ToString(), sec.Symbol));
               
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        
        }

        protected CMState ProcessSecurityList(Wrapper wrapper)
        {
            try
            {
                lock (tLock)
                {
                    SecurityList securityList = SecurityListConverter.GetSecurityList(wrapper, Config);

                    ProcessSecurities(securityList);
                    return CMState.BuildSuccess();
                }
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        protected void DoRequestMarketData()
        {
            bool active = true;
            while (active)
            {
                Thread.Sleep(SecurityListSaverConfiguration.MaxWaitingTimeForMarketDataRequest * 1000);
                  
                TimeSpan elapsed = DateTime.Now - Start;

                if (elapsed.TotalSeconds > Convert.ToDouble(SecurityListSaverConfiguration.MaxWaitingTimeForMarketDataRequest))
                {
                    foreach (string secType in MarketDataRequested.Keys)
                    {
                        if (!MarketDataRequested[secType])
                        {
                            RequestMarketData(secType);
                        }
                    }

                    active = false;
                }
            }
        }

        #endregion

        #region Public Methods

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.SECURITY_LIST)
                {
                    DoLog("Processing Security List:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);
                    return ProcessSecurityList(wrapper);
                }
                else if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    DoLog("Processing Market Data:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);
                    return ProcessMarketData(wrapper);
                }
                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), SecurityListSaverConfiguration.Name)));
            }
            catch (Exception ex)
            {
                DoLog("Error processing market data @" + SecurityListSaverConfiguration.Name + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    SecurityListConverter = new SecurityListConverter();

                    MarketDataRequested = new Dictionary<string, bool>();

                    Start = DateTime.Now;

                    //Inicializar conexión con BD y demases

                    MarketManager = new MarketManager(SecurityListSaverConfiguration.SecuritiesAccessLayerConnectionString);

                    StockManager = new StockManager(SecurityListSaverConfiguration.SecuritiesAccessLayerConnectionString);

                    OptionManager = new OptionManager(SecurityListSaverConfiguration.SecuritiesAccessLayerConnectionString);

                    CountryManager = new CountryManager(SecurityListSaverConfiguration.SecuritiesAccessLayerConnectionString);

                    StockMarketDataManager = new StockMarkeDataManager(SecurityListSaverConfiguration.SecuritiesAccessLayerConnectionString);

                    OptionMarketDataManager = new OptionMarketDataManager(SecurityListSaverConfiguration.SecuritiesAccessLayerConnectionString);

                    Markets = new List<Market>();
                    foreach(string market in SecurityListSaverConfiguration.Markets)
                    {
                        Market mrk = MarketManager.GetByCode(market);
                        if (mrk != null)
                            Markets.Add(mrk);
                        else
                        {
                            DoLog(string.Format("@{0}:Market {0} not found", market), Main.Common.Util.Constants.MessageType.Error);
                        }
                    }

                    foreach (string secType in SecurityListSaverConfiguration.SecurityTypes)
                        MarketDataRequested.Add(secType, false);

                    MainCountry = CountryManager.GetByCode(SecurityListSaverConfiguration.Country);

                    var typeMarketTranslator = Type.GetType(SecurityListSaverConfiguration.SecuritiesMarketTranslator);
                    if (typeMarketTranslator != null)
                        SecurityTranslator = (ISecurityTranslator)Activator.CreateInstance(typeMarketTranslator);
                    else
                    {
                        DoLog("assembly not found: " + SecurityListSaverConfiguration.SecuritiesMarketTranslator, Main.Common.Util.Constants.MessageType.Error);
                        return false;
                    }

                    MarketDataRequestThread = new Thread(DoRequestMarketData);
                    MarketDataRequestThread.Start();

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion
    }
}
