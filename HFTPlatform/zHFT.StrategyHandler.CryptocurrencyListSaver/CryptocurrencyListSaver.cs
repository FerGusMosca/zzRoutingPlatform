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

        protected CryptocurrencyManager CryptoCurrencyManager { get; set; }

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

        protected void UpdateCryptoMarketData(CryptoCurrency crypto, List<CryptoCurrency> notTrackedList)
        {
            try
            {
                if (!CryptosInPortfolio.Any(x => x.Symbol == crypto.Symbol))
                {
                    CryptoCurrency cryptoCurrencyData = MarketCapProviderManager.GetCryptoCurrencyData(crypto.Symbol, SecurityListSaverConfiguration.MarketCapCurrency);
                    crypto.MarketCapDesc = cryptoCurrencyData.MarketCapDesc;
                    crypto.MarketCap = cryptoCurrencyData.MarketCap;
                    notTrackedList.Add(crypto);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error retrieving market data Persisting crypto currency {1}: {2}", SecurityListSaverConfiguration.Name, crypto.Symbol, ex.Message),
                              Constants.MessageType.Error);
            }

        }

        protected CMState ProcessSecurityList(Wrapper wrapper)
        {
            try
            {
                lock (tLock)
                {
                    List<CryptoCurrency> cryptos = SecurityListConverter.GetSecurityList(wrapper, SecurityListSaverConfiguration, OnLogMsg);
                    List<CryptoCurrency> notTrackedList = new List<CryptoCurrency>();

                    DoLog(string.Format("@{0}:Processing Security List: {1} cryptos", SecurityListSaverConfiguration.Name, cryptos.Count),
                          Constants.MessageType.Information);

                    foreach (CryptoCurrency crypto in cryptos)
                    {
                        UpdateCryptoMarketData(crypto, notTrackedList);
                        DoLog(string.Format("@{0}:Persisting crypto currency {1}-{2}", SecurityListSaverConfiguration.Name, crypto.Symbol, crypto.Name),
                                Constants.MessageType.Information);

                        CryptoCurrencyManager.PersistCrypto(crypto);
                        
                    }

                    foreach (CryptoCurrency notTrackedCrypto in notTrackedList.OrderByDescending(x => x.MarketCap))
                    {
                        string msg = string.Format("RED->Cryptocurrency {0} (market cap {1}) not being tracked...", notTrackedCrypto.Symbol, notTrackedCrypto.MarketCapDesc);
                        DoLog(msg, Constants.MessageType.Information);

                    }

                    return CMState.BuildSuccess();
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}: Critical Error persisting processing cryptocurrencies: {1}", SecurityListSaverConfiguration.Name, ex.Message),
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

                    CryptoCurrencyManager = new CryptocurrencyManager(SecurityListSaverConfiguration.SecuritiesAccessLayerConnectionString);

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
