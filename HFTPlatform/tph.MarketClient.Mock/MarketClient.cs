using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using tph.MarketClient.Mock.Common.Configuration;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common;
using zHFT.MarketClient.Common.Common.Wrappers;
using zHFT.MarketClient.Common.Converters;
using zHFT.MarketClient.Common.DTO;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.StrategyHandler.DataAccessLayer;

namespace tph.MarketClient.Mock
{
    public class MarketClient : MarketClientBase, ICommunicationModule
    {

        #region Protected Attributes

        protected Configuration Configuration { get; set; }

        protected CandleManager CandleManager { get; set; }

        protected object tObject { get; set; }

        #endregion


        #region Protected Methods

        protected void ProcessHistoricalDataRequestAsync(object param)
        {
            try
            {

                lock (tObject)
                {

                    Wrapper histPrWrapper = (Wrapper)param;

                    HistoricalPricesRequestDTO dto = HistoricalPriceConverter.ConvertHistoricalPriceRequest(histPrWrapper);

                    if (!dto.From.HasValue && !dto.To.HasValue)
                    {
                        throw new Exception($"Historical Prices Request must have From AND To Specified");
                    }
                    else
                    {
                        TimeSpan distance = dto.To.Value - dto.From.Value ;

                        dto.From = Configuration.From.AddMinutes(-1 * distance.TotalMinutes);
                        dto.To = Configuration.From;
                    
                    }

                    List<MarketData> candles=  CandleManager.GetCandles(dto.Symbol, dto.Interval, dto.From.Value, dto.To.Value);


                    Security mainSec = new Security() { Symbol = dto.Symbol, Currency = dto.Currency, SecType = dto.SecurityType };

                    List<Wrapper> marketDatWrList = new List<Wrapper>();
                    foreach (MarketData candle in candles)
                    {
                        Security sec = new Security() { Symbol = dto.Symbol, Currency = dto.Currency, SecType = dto.SecurityType };
                        sec.MarketData = candle;
                        MarketDataWrapper mdWrp = new MarketDataWrapper(sec, Configuration);
                        marketDatWrList.Add(mdWrp);
                    }

                    HistoricalPricesWrapper histWrp = new HistoricalPricesWrapper(dto.ReqId, mainSec, dto.Interval, marketDatWrList);
                    
                    (new Thread(OnPublishAsync)).Start(histWrp);

                }


            }
            catch (Exception ex)
            {
                DoLog($"CRITICAL error requesting Historical Prices : {ex.Message}", Constants.MessageType.Error);
            }
        
        }

        #endregion

        #region Interface/Abstract Methods

        public bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {

                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    tObject = new object();
                    CandleManager = new CandleManager(Configuration.ConnectionString);

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Constants.MessageType.Error);
                return false;
            }
        }

        public CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    if (Actions.MARKET_DATA_REQUEST == action)
                    {
                        //return ProcessMarketDataRequest(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else if (Actions.HISTORICAL_PRICES_REQUEST == action)
                    {
                        string symbol = (string)wrapper.GetField(HistoricalPricesRequestFields.Symbol);
                        DoLog($"{Configuration.Name}: Recv Historical Prices Request for symbol {symbol}", Constants.MessageType.Information);
                        (new Thread(ProcessHistoricalDataRequestAsync)).Start(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else if (Actions.SECURITY_LIST_REQUEST == action)
                    {
                        DoLog($"{Configuration.Name}: Recv Security List Request ", Constants.MessageType.Information);
                        //return ProcessSecurityListRequest(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else
                    {
                        DoLog(string.Format("@{0}:Sending message {1} not implemented", Configuration.Name, action.ToString()), Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception(string.Format("@{0}:Sending message {1} not implemented", Configuration.Name, action.ToString())));
                    }
                }
                else
                    throw new Exception("Invalid Wrapper");


            }
            catch (Exception ex)
            {
                DoLog(ex.Message, Constants.MessageType.Error);
                throw;
            }
        }

        protected override void DoLoadConfig(string configFile, List<string> noValueFields)
        {
            Configuration = new Configuration().GetConfiguration<Configuration>(configFile, noValueFields);
        }

        protected override IConfiguration GetConfig()
        {
            return Configuration;
        }

        #endregion
    }
}
