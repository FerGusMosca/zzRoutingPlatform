using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.BusinessEntities.Security_List;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Converters;
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

        protected CountryManager CountryManager { get; set; }

        Market MainMarket { get; set; }

        Country MainCountry { get; set; }

        #endregion

        #region ProtectedMethods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new zHFT.StrategyHandler.SecurityListSaver.Common.Configuration.Configuration().GetConfiguration<zHFT.StrategyHandler.SecurityListSaver.Common.Configuration.Configuration>(configFile, noValueFields);
        }

        #endregion

        #region Private Methods

        protected void ProcessStocks(SecurityList securityList)
        {
            SecurityTranslator.DoTranslate(securityList);

            List<Security> stocksSecurities = securityList.Securities.Where(x => x.SecType == SecurityType.CS).ToList();

            //solo trabajamos con el main market
            foreach (Security security in stocksSecurities.Where(x => x.Exchange == MainMarket.Code))
            {
                Stock stock = new Stock();
                stock.Market = MainMarket;
                stock.Country = MainCountry.Code;
                stock.Symbol = security.Symbol.Trim();
                stock.Name = "";
                stock.Category = "";

                Stock prevStock = StockManager.GetByCode(security.Symbol.Trim(), MainMarket.Code, MainCountry.Code);

                if (prevStock == null)
                {
                    stock.LoadFinalSymbol();
                    StockManager.Persist(stock);
                    DoLog(string.Format("Inserting new stock from market {0}:", stock.Symbol),
                          Main.Common.Util.Constants.MessageType.Information);

                }
                else
                {
                    DoLog(string.Format("Stock {0} already existed", stock.Symbol),
                             Main.Common.Util.Constants.MessageType.Information);
                }
            }
        }

        protected CMState ProcessSecurityList(Wrapper wrapper)
        {

            try
            {
                SecurityList securityList =  SecurityListConverter.GetSecurityList(wrapper, Config);

                //Procesar y guardar
                //List<Security> options = securityList.Securities.Where(x => x.SecType == SecurityType.OPT).ToList();
                ProcessStocks(securityList);
                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        #endregion

        #region Public Methods

        public override Main.Common.DTO.CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.SECURITY_LIST)
                {
                    DoLog("Processing Security List:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);
                    return ProcessSecurityList(wrapper);
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
                    //Inicializar conexión con BD y demases

                    MarketManager = new MarketManager(SecurityListSaverConfiguration.SecuritiesAccessLayerConnectionString);

                    StockManager = new StockManager(SecurityListSaverConfiguration.SecuritiesAccessLayerConnectionString);

                    CountryManager = new CountryManager(SecurityListSaverConfiguration.SecuritiesAccessLayerConnectionString);

                    MainMarket= MarketManager.GetByCode(SecurityListSaverConfiguration.Market);

                    MainCountry = CountryManager.GetByCode(SecurityListSaverConfiguration.Country);


                    if (MainMarket == null)
                        throw new Exception(string.Format("No hay ningún mercado configurado para el código {0}", SecurityListSaverConfiguration.Market));


                    var typeMarketTranslator = Type.GetType(SecurityListSaverConfiguration.SecuritiesMarketTranslator);
                    if (typeMarketTranslator != null)
                        SecurityTranslator = (ISecurityTranslator)Activator.CreateInstance(typeMarketTranslator);
                    else
                    {
                        DoLog("assembly not found: " + SecurityListSaverConfiguration.SecuritiesMarketTranslator, Main.Common.Util.Constants.MessageType.Error);
                        return false;
                    }

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
