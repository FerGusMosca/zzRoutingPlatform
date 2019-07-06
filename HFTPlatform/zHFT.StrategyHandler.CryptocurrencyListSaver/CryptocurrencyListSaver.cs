using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.CryptocurrencyListSaver.BusinessEntitie;
using zHFT.StrategyHandler.CryptocurrencyListSaver.BusinessEntities;
using zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Converters;
using zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Interface;
using zHFT.StrategyHandler.CryptocurrencyListSaver.DataAccessLayer.ADO;
using zHFT.StrategyHandler.CryptocurrencyListSaver.DataAccessLayer.Managers;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver
{
    public class CryptocurrencyListSaver : BaseCommunicationModule
    {
        #region Protected Attributes

        protected zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Configuration.Configuration SecurityListSaverConfiguration
        {
            get { return (zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected DateTime Start { get; set; }

        protected SecurityListConverter SecurityListConverter { get; set; }

        protected CryptoCurrencyManager CryptoCurrencyManager { get; set; }

        protected CryptoInPortolioManager CryptoInPortolioManager { get; set; }

        protected List<CryptoInPortfolio> CryptosInPortfolio { get; set; }

        protected IMarketCapProvider MarketCapProviderManager { get; set; }

        #endregion

        #region Protected Methods
        
        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Configuration.Configuration().GetConfiguration<zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Configuration.Configuration>(configFile, noValueFields);

        }

        protected CMState ProcessSecurityList(Wrapper wrapper)
        {
            try
            {
                lock (tLock)
                {
                    List<CryptoCurrency> cryptos = SecurityListConverter.GetSecurityList(wrapper, SecurityListSaverConfiguration, OnLogMsg);
                    List<CryptoCurrency> notTracked = new List<CryptoCurrency>();

                    DoLog(string.Format("@{0}:Processing Security List: {1} cryptos", SecurityListSaverConfiguration.Name, cryptos.Count),
                          Constants.MessageType.Information);

                    foreach (CryptoCurrency crypto in cryptos)
                    {

                        if (!CryptosInPortfolio.Any(x => x.Symbol == crypto.Symbol))
                        {
                            decimal marketCap = MarketCapProviderManager.GetMarketCap(crypto.Symbol, SecurityListSaverConfiguration.MarketCapCurrency);
                            crypto.MarketCap = marketCap;
                            notTracked.Add(crypto);
                        }

                        DoLog(string.Format("@{0}:Persisting crypto currency {1}-{2}", SecurityListSaverConfiguration.Name, crypto.Symbol, crypto.Name),
                              Constants.MessageType.Information);

                        CryptoCurrencyManager.Persist(crypto);
                    }

                    foreach (CryptoCurrency notTrackedCrypto in notTracked.OrderByDescending(x => x.MarketCap))
                    {
                        string msg = string.Format("RED->Cryptocurrency {0} (market cap {1}) not being tracked...", notTrackedCrypto.Symbol, notTrackedCrypto.MarketCap);
                        DoLog(msg, Constants.MessageType.Information);

                    }

                    return CMState.BuildSuccess();
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error persisting Persisting crypto currency: {0}", SecurityListSaverConfiguration.Name, ex.Message),
                              Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        #endregion

        public override CMState ProcessMessage(Wrapper wrapper)
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

        public override bool Initialize(Main.Common.Interfaces.OnMessageReceived pOnMessageRcv, Main.Common.Interfaces.OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    Start = DateTime.Now;

                    SecurityListConverter = new SecurityListConverter();

                    CryptoCurrencyManager = new CryptoCurrencyManager(SecurityListSaverConfiguration.SecuritiesAccessLayerConnectionString);

                    CryptoInPortolioManager = new DataAccessLayer.ADO.CryptoInPortolioManager(SecurityListSaverConfiguration.CryptosInPortfolioConnectionString);

                    CryptosInPortfolio = CryptoInPortolioManager.GetCryptoInPortfolio();


                    if (!string.IsNullOrEmpty(SecurityListSaverConfiguration.MarketCapProvider))
                    {
                        var marketCapProviderType = Type.GetType(SecurityListSaverConfiguration.MarketCapProvider);
                        if (marketCapProviderType != null)
                        {
                            MarketCapProviderManager = (IMarketCapProvider)Activator.CreateInstance(marketCapProviderType, SecurityListSaverConfiguration.MarketCapProviderAPIKey, SecurityListSaverConfiguration.MarketCapProviderURL);
                        }
                        else
                            throw new Exception("assembly not found: " + SecurityListSaverConfiguration.MarketCapProvider);
                    }
                    else
                        DoLog("Market Cap Provider proxy not found. It will not be initialized", Constants.MessageType.Error);


                    //Ya arrancamos pidiendo el security list
                    SecurityListRequestWrapper wrapper = new SecurityListRequestWrapper(SecurityListRequestType.AllSecurities, null);
                    OnMessageRcv(wrapper);

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
    }
}
