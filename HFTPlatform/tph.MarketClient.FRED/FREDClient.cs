using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.MarketClient.FRED.Common;
using Xaye.Fred;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.DTO;
using zHFT.StrategyHandler.Common.Wrappers;

namespace tph.MarketClient.FRED
{
    public class FREDClient : MarketClientBase, ICommunicationModule
    {

        #region Protected Attributes

        public IConfiguration Config { get; set; }
        protected FREDConfiguration FREDConfiguration
        {
            get { return (FREDConfiguration)Config; }
            set { Config = value; }
        }

        #endregion

        #region Public Methods
        protected override void DoLoadConfig(string configFile, List<string> noValueFields)
        {
            
            Config = new FREDConfiguration().GetConfiguration<FREDConfiguration>(configFile, noValueFields);
        }

        protected override IConfiguration GetConfig()
        {
            return Config;
        }

        #endregion

        #region  Protected Methods

        protected CMState ProcessEconomicSeriesRequest(Wrapper wrapper)
        {

            try
            {

                EconomicSeriesRequestDTO dto = EconomicSeriesRequestConverter.ConvertEconomicSeriesRequest(wrapper);


                DoLog($"{FREDConfiguration.Name}--> Received Econmic Series Req. for SeriesID {dto.SeriesID}: From={dto.From} To={dto.To}", Constants.MessageType.Information);

                var fredAPIClient = new Fred(FREDConfiguration.APIKey);

                var result = fredAPIClient.GetSeriesObservations(dto.SeriesID,dto.From,dto.To);

                List<EconomicSeriesValue> economicSeries = new List<EconomicSeriesValue>();

                foreach (Observation obser in result)
                {
                    EconomicSeriesValue value = new EconomicSeriesValue()
                    {
                        Date = obser.Date,
                        Value = obser.Value
                    };

                    economicSeries.Add(value);
                }

                DoLog($"{FREDConfiguration.Name}--> Puublic Series w/{economicSeries.Count} records for SeriesID {dto.SeriesID}: From={dto.From} To={dto.To}", Constants.MessageType.Information);

                //TODO eval publish async
                EconomicSeriesWrapper economicSeriesWrp = new EconomicSeriesWrapper(dto.SeriesID, dto.From, dto.To, economicSeries);

                OnMessageRcv(economicSeriesWrp);

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {

                DoLog($"{FREDConfiguration.Name}--> ERROR processing Econmic Series Req.:{ex.Message}", Constants.MessageType.Error);

                return CMState.BuildFail(ex);
            
            }
        }

        #endregion


        #region ICommunicationModule
        bool ICommunicationModule.Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {

                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {

                    //TestFREDTresuryYields();

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

        CMState ICommunicationModule.ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    if (Actions.ECONOMIC_SERIES_REQUEST == action)
                    {
                        return ProcessEconomicSeriesRequest(wrapper);
                    }
                   
                    else
                    {
                        DoLog(string.Format("@{0}:Sending message {1} not implemented", FREDConfiguration.Name, action.ToString()), Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception(string.Format("@{0}:Sending message {1} not implemented", FREDConfiguration.Name, action.ToString())));
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


        #endregion
    }
}
