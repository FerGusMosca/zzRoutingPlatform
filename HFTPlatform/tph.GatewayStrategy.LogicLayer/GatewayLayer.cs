using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.LogicLayer;
using zHFT.Main.BusinessEntities.Market_Data;
using static zHFT.Main.Common.Util.Constants;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandlers.Common.Converters;
using System.Threading;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;
using tph.GatewayStrategy.Common.Configuration;

namespace tph.GatewayStrategy.LogicLayer
{
    public class GatewayLayer : DayTradingStrategyBase
    {
        #region Protected Attributes

        protected GatewayConfiguration Config { get;set; }

        protected Dictionary<string, MarketData> MarketDataDict { get; set; }

        #endregion


        #region Overriden Methods

        public override void InitializeManagers(string connStr)
        {
            //No managers-No DB
        }

        protected override zHFT.StrategyHandler.BusinessEntities.PortfolioPosition DoOpenTradingFuturePos(zHFT.Main.BusinessEntities.Positions.Position pos, zHFT.StrategyHandler.BusinessEntities.MonitoringPosition portfPos)
        {
            throw new NotImplementedException("No Futures Trading Logic");
        }

        protected override zHFT.StrategyHandler.BusinessEntities.PortfolioPosition DoOpenTradingRegularPos(zHFT.Main.BusinessEntities.Positions.Position pos, zHFT.StrategyHandler.BusinessEntities.MonitoringPosition portfPos)
        {
            PositionWrapper posWrapper = new PositionWrapper(pos, Config);
            OrderRouter.ProcessMessage(posWrapper);
            return null;
        }

        protected override void DoPersist(zHFT.StrategyHandler.BusinessEntities.PortfolioPosition trdPos)
        {
            //No managers no DB
        }

        protected override void LoadMonitorsAndRequestMarketData()
        {

            try
            {

                DoLog($"Initializing Gateway Layer...", MessageType.PriorityInformation);
                //We load different entities
                MarketDataDict = new Dictionary<string, MarketData>();


                OrderRouter = LoadModules(Config.OrderRouter, Config.OrderRouterConfigFile, DoLog);

                DoLog($"Gateway Layer successfully initialized...", MessageType.PriorityInformation);
            }
            catch (Exception ex) {

                DoLog($"CRITICAL ERROR initializing Gateway Layer: {ex.Message}", MessageType.Error);
            
            }

        }

        protected override void LoadPreviousTradingPositions()
        {
            //Nothing to Load
        }

        protected override void ProcessHistoricalPrices(object pWrapper)
        {
            //Nothing to Process
        }

        protected override void ProcessMarketData(object pWrapper)
        {
            Wrapper wrapper = (Wrapper)pWrapper;
            MarketData md = zHFT.StrategyHandler.Common.Converters.MarketDataConverter.ConvertMarketData(wrapper);
            lock (MarketDataDict)
            {

                if (!MarketDataDict.ContainsKey(md.Security.Symbol))
                {
                    DoLog($"Recv first MD for symbol {md.Security.Symbol}:{md.ToString()}", MessageType.PriorityInformation);
                    MarketDataDict.Add(md.Security.Symbol, md);
                }
                else
                {
                    MarketDataDict[md.Security.Symbol] = md;
                }

            }

            OrderRouter.ProcessMessage(wrapper);
        }

        protected override void ResetEveryNMinutes(object param)
        {

        }

        protected override void DoRequestSecurityListThread(object param)
        { 
        
        
        }

        public override  CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.NEW_ORDER)
                {
                    //TODO Transform into a NEW_POSITION and send to Gen Order Roeuter
                    return CMState.BuildSuccess();
                }
                else
                    return base.ProcessMessage(wrapper);
            }
            catch (Exception ex)
            {
                DoLog($"ERROR @ProcessMessage of GatewayLayer {Config.Name} : {ex.Message}",MessageType.Error);
                return CMState.BuildFail(ex);
            }

        }

        #endregion


        #region Gateway Methods


        //Nothing to process on the Gateway from the incoming app which is not a message
        public CMState ProcessIncoming(Wrapper wrapper)
        {
            try
            {
                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog($"{Config.Name}-->Error processing market data: {ex.Message} ", Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }



        //From the exchange ---> We send it to the Incoming Module (outside app)
        protected override CMState ProcessOutgoing(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                    DoLog($"Incoming message from order routing w/ Action {wrapper.GetAction()}: " + wrapper.ToString(), Constants.MessageType.Information);

                return base.ProcessOutgoing(wrapper);
            }
            catch (Exception ex)
            {

                DoLog("Error processing message from order routing: " + (wrapper != null ? wrapper.ToString() : "") + " Error:" + ex.Message, Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }


        public override void DoLoadConfig(string configFile, List<string> noValFields)
        {

            List<string> noValueFields = new List<string>();
            Config = new GatewayConfiguration().GetConfiguration<GatewayConfiguration>(configFile, noValueFields);
        }


        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {

            OnLogMsg += pOnLogMsg;

            if (ConfigLoader.LoadConfig(this, configFile))
            {
                base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);
                return true;
            }
            else
            {
                DoLog($"Error initializing config file at GatewayLayer!", MessageType.Error);
                return false;
            
            }
        }

        #endregion
    }

}
