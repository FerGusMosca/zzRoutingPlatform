using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.LogicLayer;
using zHFT.StrategyHandler.OptionsContractSaver.BusinessEntities;
using zHFT.StrategyHandler.OptionsContractSaver.Common.Configuration;
using zHFT.StrategyHandler.OptionsContractSaver.DataAccessLayer.Managers;

namespace zHFT.StrategyHandler.OptionsContractSaver
{
    public class OptionsContractSaver : ICommunicationModule
    {

        #region Protected Attributes

        protected OnMessageReceived OnMessageRcv { get; set; }
        protected OnLogMessage OnLogMsg { get; set; }

        protected object tLock = new object();

        protected Configuration OCSConfiguration { get; set; }

        protected OptionBidManager OptionBidManager { get; set; }

        protected OptionManager OptionManager { get; set; }

        protected Dictionary<string, Option> ActiveOptions { get; set; }

        #endregion

        #region Protected Methods

        protected void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        protected void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            OCSConfiguration = new Configuration().GetConfiguration<Configuration>(configFile, noValueFields);
        }

        protected bool LoadConfig(string configFile)
        {
            DoLog(DateTime.Now.ToString() + "MarketClientBase.LoadConfig", Constants.MessageType.Information);

            DoLog("Loading config:" + configFile, Constants.MessageType.Information);
            if (!File.Exists(configFile))
            {
                DoLog(configFile + " does not exists", Constants.MessageType.Error);
                return false;
            }

            List<string> noValueFields = new List<string>();
            DoLog("Processing config:" + configFile, Constants.MessageType.Information);
            try
            {
                DoLoadConfig(configFile, noValueFields);
                DoLog("Ending GetConfiguracion " + configFile, Constants.MessageType.Information);
            }
            catch (Exception e)
            {
                DoLog("Error recovering config " + configFile + ": " + e.Message, Constants.MessageType.Error);
                return false;
            }

            if (noValueFields.Count > 0)
                noValueFields.ForEach(s => DoLog(string.Format(Constants.FieldMissing, s), Constants.MessageType.Error));

            return true;
        }

        private Option GetOptionBySymbol(string symbol)
        {
            if (!ActiveOptions.Keys.Contains(symbol))
            {
                Option opt = OptionManager.GetActiveOptionBySymbol(symbol);

                if (opt != null)
                {
                    ActiveOptions.Add(symbol, opt);
                    return opt;
                }
                else
                    throw new Exception(string.Format("Could not find option for symbol {0}", symbol));

            }
            else
                return ActiveOptions[symbol];
        
        }

        protected  CMState ProcessMarketData(Wrapper wrapper)
        {
            lock (tLock)
            {
                try
                {
                    if (wrapper.GetField(MarketDataFields.BestBidSize) != null
                        && wrapper.GetField(MarketDataFields.BestBidPrice) != null
                        && wrapper.GetField(MarketDataFields.CompositeUnderlyingPrice) != null)
                    {
                        
                        OptionBid bid = new OptionBid()
                        {
                            Option = GetOptionBySymbol (wrapper.GetField(MarketDataFields.Symbol).ToString() ) ,
                            Side = Side.Buy,
                            Size = Convert.ToInt32(wrapper.GetField(MarketDataFields.BestBidSize)),
                            Timestamp = DateTime.Now,
                            Price = Convert.ToDecimal(wrapper.GetField(MarketDataFields.BestBidPrice)),
                            UnderlyingPrice = Convert.ToDecimal(wrapper.GetField(MarketDataFields.CompositeUnderlyingPrice))
                        };

                        OptionBidManager.Persist(bid);
                    }


                    if (wrapper.GetField(MarketDataFields.BestAskSize) != null
                        && wrapper.GetField(MarketDataFields.BestAskPrice) != null
                        && wrapper.GetField(MarketDataFields.CompositeUnderlyingPrice) != null)
                    {

                        OptionBid ask = new OptionBid()
                        {
                            Option = GetOptionBySymbol(wrapper.GetField(MarketDataFields.Symbol).ToString()),
                            Side = Side.Sell,
                            Size = Convert.ToInt32(wrapper.GetField(MarketDataFields.BestAskSize)),
                            Timestamp = DateTime.Now,
                            Price = Convert.ToDecimal(wrapper.GetField(MarketDataFields.BestAskPrice)),
                            UnderlyingPrice = Convert.ToDecimal(wrapper.GetField(MarketDataFields.CompositeUnderlyingPrice))
                        };

                        OptionBidManager.Persist(ask);
                    }

                    return CMState.BuildSuccess();
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{0} Error processing market data : ", OCSConfiguration.Name, ex.Message), Constants.MessageType.Error);
                    return CMState.BuildFail(ex);
                }
            }
        }

        #endregion

        #region Public Methods

        public CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    DoLog("Processing Market Data:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);
                    return ProcessMarketData(wrapper);
                }
                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), OCSConfiguration.Name)));
            }
            catch (Exception ex)
            {
                DoLog("Error processing market data @" + OCSConfiguration.Name + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        public bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    OptionBidManager = new OptionBidManager(OCSConfiguration.EFOptionsAccessLayerConnectionString,OCSConfiguration.OptionsAccessLayerConnectionString);

                    OptionManager = new OptionManager(OCSConfiguration.EFOptionsAccessLayerConnectionString);

                    ActiveOptions = new Dictionary<string, Option>();

                    DoLog("Option Contract Saver sucessfully initialized " , Main.Common.Util.Constants.MessageType.Information);

                    return true;

                }
                else
                    return false;
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
