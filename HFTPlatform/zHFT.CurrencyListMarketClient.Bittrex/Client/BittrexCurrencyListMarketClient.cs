using Bittrex;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.CurrencyListMarketClient.Bittrex.BusinessEntities;
using zHFT.CurrencyListMarketClient.Bittrex.Common.Wrappers;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.CurrencyListMarketClient.Bittrex.Client
{
    public class BittrexCurrencyListMarketClient : BaseCommunicationModule
    {
        #region Protected Attributes

        public Exchange Exchange { get; set; }

        public ExchangeContext ExchangeContext { get; set; }

        protected Bittrex.Common.Configuration.Configuration BittrexConfiguration
        {
            get { return (Bittrex.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }


        #endregion

        #region Protected Methods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Bittrex.Common.Configuration.Configuration().GetConfiguration<Bittrex.Common.Configuration.Configuration>(configFile, noValueFields);

        }

        protected ExchangeContext GetContext()
        {
            return new ExchangeContext()
            {
                ApiKey = BittrexConfiguration.ApiKey,
                QuoteCurrency = BittrexConfiguration.QuoteCurrency,
                Secret = BittrexConfiguration.Secret,
                Simulate = BittrexConfiguration.Simulate
            };
        }

        protected void DoRequestSecurityList(object param)
        {
            try
            {
                lock (tLock)
                {
                    JArray jCurrencies  = Exchange.GetCurrencies();

                    List<CryptoCurrency> items = ((JArray)jCurrencies).Select(x => new CryptoCurrency
                    {
                        Symbol = (string)x["Currency"],
                        Name = (string)x["CurrencyLong"],
                        MinConfirmation = (double)x["MinConfirmation"],
                        TxFee = (double)x["TxFee"],
                        IsActive = (bool)x["IsActive"],
                        CoinType = (string)x["CoinType"],
                        BaseAddress = (string)x["BaseAddress"],
                        Notice = (string)x["Notice"],


                    }).ToList();

                    SecurityListWrapper wrapper = new SecurityListWrapper(items, BittrexConfiguration);
                    OnMessageRcv(wrapper);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}: Error Requesting For Security List:{1}", BittrexConfiguration.Name, ex.Message),
                      Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected CMState ProcessSecurityListRequest(Wrapper wrapper)
        {
            lock (tLock)
            {
                Thread securityListRequestThread = new Thread(DoRequestSecurityList);
                securityListRequestThread.Start(wrapper);
                return CMState.BuildSuccess();
            }
        }

        #endregion

        #region Public Methods
        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    if (action == Actions.SECURITY_LIST_REQUEST)
                    {
                        return ProcessSecurityListRequest(wrapper);
                    }
                    else
                    {
                        DoLog("Sending message " + action + " not implemented", Main.Common.Util.Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception("Sending message " + action + " not implemented"));
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

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    Exchange = new Exchange();

                    ExchangeContext = GetContext();

                    Exchange.Initialise(ExchangeContext);

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
