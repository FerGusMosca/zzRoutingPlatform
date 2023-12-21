using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.StrategyHandler.HistoricalPricesDownloader.Common.Configuration;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Configuration;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Common.Wrappers;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.DTO;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.DataAccessLayer;
using zHFT.StrategyHandler.LogicLayer;
using static zHFT.Main.Common.Util.Constants;

namespace tph.StrategyHandler.HistoricalPricesDownloader
{
    public class HistoricalPricesDownloader : ICommunicationModule, ILogger
    {

        #region Protected Attributes

        protected HistoricalPricesDownloaderConfiguration Config { get; set; }

        public string ModuleConfigFile { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected OnMessageReceived OnMessageRcv { get; set; }

        protected Dictionary<int,string> HistoricalPricesRequests { get; set; }

        protected CandleManager CandleManager { get; set; }

        #endregion


        #region Protected Methods

        protected CMState ProcessHistoricalPrices(Wrapper wrapper)
        {
            try
            {

                HistoricalPricesWrapper histWrapper = (HistoricalPricesWrapper)wrapper;

                zHFT.StrategyHandler.Common.DTO.HistoricalPricesDTO dto = HistoricalPricesConverter.ConvertHistoricalPrices(histWrapper);


                if (HistoricalPricesRequests.ContainsKey(dto.ReqId))
                {
                    DoLog($"{Config.Name}-> Processing historical prices for symbol {dto.Symbol}:{dto.MarketData.Count} prices found", MessageType.Information);

                    foreach (MarketData md in dto.MarketData)
                    {
                        DoLog($"{Config.Name} Persisting market data for date {md.GetDateTime()}:{md.ToString()}", MessageType.Information);
                        CandleManager.Persist(dto.Symbol, dto.Interval, md);

                    }


                    DoLog($"{Config.Name}--> All the pricess were succesfully persisted", MessageType.Information);
                }
                else {

                    DoLog($"{Config.Name}--> Ignoring unknwon historical price for request Id {dto.ReqId}", MessageType.Information);
                
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog($"{Config.Name}--> CRITICAL ERROR Processing historical prices:{ex.Message}",MessageType.Error);
                return CMState.BuildFail(ex);
            
            }
        }

        protected void DoSendAsync(object param)
        {
            try
            {
                Wrapper wrapper=(Wrapper)param;
                OnMessageRcv(wrapper);
            }
            catch (Exception ex) {

                DoLog($"{Config.Name}--> CRITICAL ERROR Sending Async Message: {ex.Message}",MessageType.Error);
            }
        
        }

        protected void RequestHistoricalPrices()
        {
            try
            {
                DateTime to = Config.To.Value.AddDays(1);
                DateTime from = Config.From.Value;
                SecurityType secType = Security.GetSecurityType(Config.SecurityType);
                CandleInterval interval = Config.GetCandleInterval() ;

                Thread.Sleep(Config.PacingOnConnections);

                foreach (string symbol in Config.SymbolsToDownload)
                {
                    TimeSpan elapsed = DateTimeManager.Now - new DateTime(1970, 1, 1);
                    int reqId = Convert.ToInt32(elapsed.TotalSeconds);   

                    DoLog($"{Config.Name}--> Requesting Historical Prices for Symbol {symbol} From={from} To={to}", MessageType.Information);
                    HistoricalPricesRequestWrapper wrapper = new HistoricalPricesRequestWrapper(
                        reqId,symbol, from, to, interval,Config.Currency, secType,Config.Exchange);

                    HistoricalPricesRequests.Add(reqId, symbol);
                    (new Thread(DoSendAsync)).Start(wrapper);
                    
                    Thread.Sleep(Config.PacingBtwRequests);//Some pacing for safety
                }
               
            }
            catch (Exception ex)
            {
                DoLog($"{Config.Name}--> CRITICAL ERROR requesting historical prices:{ex.Message}", MessageType.Error);
            }
        }

        #endregion

        #region Interface Methods
        public void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            Config = ConfigLoader.GetConfiguration<HistoricalPricesDownloaderConfiguration>(this, configFile, listaCamposSinValor);
        }

        public void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(string.Format("{0}", msg), type);
        }

        public bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            this.ModuleConfigFile = configFile;
            this.OnMessageRcv += pOnMessageRcv;
            this.OnLogMsg += pOnLogMsg;
            

            if (ConfigLoader.LoadConfig(this, configFile))
            {

                HistoricalPricesRequests = new Dictionary<int, string>();
                CandleManager = new CandleManager(Config.ConnectionString);

                RequestHistoricalPrices();

                return true;

            }
            else
            {
                return false;
            }
        }

        public CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    DoLog($"{Config.Name}-->Processing Market Data Not implemented" , MessageType.Error);
                    //ProcessMarketData(wrapper);
                    return CMState.BuildSuccess();
                }

                if (wrapper.GetAction() == Actions.HISTORICAL_PRICES)
                {
                    ProcessHistoricalPrices(wrapper);
                    return CMState.BuildSuccess();
                }
                else if (wrapper.GetAction() == Actions.SECURITY_LIST)
                {
                    //ProcessSecurityList(wrapper);
                    DoLog($"{Config.Name}-->Processing Security List Not implemented", MessageType.Error);
                    return CMState.BuildSuccess();
                }
                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), Config.Name)));
            }
            catch (Exception ex)
            {
                DoLog($"ERROR @ProcessMessage {Config.Name} : {ex.Message}", MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        #endregion



    }
}
