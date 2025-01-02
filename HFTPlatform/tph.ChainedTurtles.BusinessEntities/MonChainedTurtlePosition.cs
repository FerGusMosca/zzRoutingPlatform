using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.DTO;
using tph.ChainedTurtles.Common.Interfaces;
using tph.ChainedTurtles.Common.Util;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Util;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.BusinessEntities;
using static zHFT.Main.Common.Util.Constants;

namespace tph.ChainedTurtles.BusinessEntities
{
    public class MonChainedTurtlePosition: MonTrendlineTurtlesPosition, IMonPosition
    {

        #region Protected Attributes

        protected List<MonTurtlePosition> InnerIndicators { get; set; }

        #endregion

        #region Constructor 

        public MonChainedTurtlePosition(Security pSecurity,TurtlesCustomConfig pTurtlesCustomConfig, 
                                        MonitoringType pMonitoringType,ILogger pLogger=null):base(pTurtlesCustomConfig,0,null)
        {

            Security = pSecurity;

            Logger = pLogger;

            InnerIndicators = new List<MonTurtlePosition>();

            LoadConfigValues(pTurtlesCustomConfig.CustomConfig);//This is where Stop Loss and CandleRef price are loaded

            MonitoringType = pMonitoringType;
        }

        #endregion

        #region Protected Attributes

        protected int HistoricalPricesPeriod { get; set; }

        public bool RequestHistoricalPrices { get; set; }

        public bool CloseOnInnnerIndicators { get; set; }

        protected MonPosInnerIndicatorsOrchestationLogicDTO OrchestationLogic { get; set; }

        #endregion


        #region Private Methods

        private void LoadConfigValues(string customConfig)
        {
            //
            try
            {
                MonPosTrutleIndicatorConfigDTO resp = JsonConvert.DeserializeObject<MonPosTrutleIndicatorConfigDTO>(customConfig);

                MarketStartTime = TurtleIndicatorBaseConfigLoader.GetMarketStartTime(resp);
                MarketEndTime = TurtleIndicatorBaseConfigLoader.GetMarketEndTime(resp);
                ClosingTime = TurtleIndicatorBaseConfigLoader.GetClosingTime(resp);
                HistoricalPricesPeriod = TurtleIndicatorBaseConfigLoader.GetHistoricalPricesPeriod(resp);
                RequestHistoricalPrices = TurtleIndicatorBaseConfigLoader.GetRequestHistoricalPrices(resp,true);

                CloseOnInnnerIndicators = resp.DoCloseOnInnnerIndicators();

                if (!string.IsNullOrEmpty(resp.candleReferencePrice))
                {
                    CandleReferencePrice = resp.candleReferencePrice;
                }
                else
                    throw new Exception("Missing config value candleReferencePrice");

                if (resp.stopLossForOpenPositionPct >= 0)
                    StopLossForOpenPositionPct = resp.stopLossForOpenPositionPct;
                else
                    throw new Exception("config value stopLossForOpenPositionPct must be greater or equal than 0");

                if (resp.innerIndicatorsOrchestationLogic != null)
                {
                    resp.innerIndicatorsOrchestationLogic.ValidateOrchestationLogic(Security.Symbol);
                    OrchestationLogic = resp.innerIndicatorsOrchestationLogic;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"CRITICAL error deserializing custom config for symbol {Security.Symbol}:{ex.Message} ");
            }
        }


        private bool AllIndicatorsLongSignal()
        {
            bool allOn = true;
            DoLog($"ALL_IND_DBG1-Evaluating All indicator LONG for symbol {Security.Symbol}", zHFT.Main.Common.Util.Constants.MessageType.Information);
            foreach (var indicator in InnerIndicators)
            {
                DoLog($"ALL_IND_DBG2-Evaluating  LONG indicator  {indicator.Security.Symbol}", zHFT.Main.Common.Util.Constants.MessageType.Information);
                if (!indicator.LongSignalTriggered())
                {
                    DoLog($"ALL_IND_DBG3-INDICATOR IS FALSE discarding LONG signal", zHFT.Main.Common.Util.Constants.MessageType.Information);
                    allOn = false;
                }

            }

            DoLog($"ALL_IND_DBG4-INDICATOR Final LONG result for security {Security.Symbol}:allOn={allOn} InnerInd={InnerIndicators.Count}", zHFT.Main.Common.Util.Constants.MessageType.Information);
            return InnerIndicators.Count > 0 && allOn;

        }

        private bool AllIndicatorsShortSignal()
        {
            bool allOn = true;
            DoLog($"ALL_IND_DBG1-Evaluating All indicator SHORT for symbol {Security.Symbol}", zHFT.Main.Common.Util.Constants.MessageType.Information);
            foreach (var indicator in InnerIndicators)
            {
                DoLog($"ALL_IND_DBG2-Evaluating  SHORT indicator  {indicator.Security.Symbol}", zHFT.Main.Common.Util.Constants.MessageType.Information);
                if (!indicator.ShortSignalTriggered())
                {
                    DoLog($"ALL_IND_DBG3-INDICATOR IS FALSE discarding SHORT signal", zHFT.Main.Common.Util.Constants.MessageType.Information);
                    allOn = false;
                }

            }
            DoLog($"ALL_IND_DBG4-INDICATOR Final SHORT result for security {Security.Symbol}:allOn={allOn} InnerInd={InnerIndicators.Count}", zHFT.Main.Common.Util.Constants.MessageType.Information);
            return InnerIndicators.Count > 0 &allOn;

        }


        private bool FirstIndicatorLongSignal()
        {
            bool oneTrue = false;
            foreach (var indicator in InnerIndicators)
            {
                if (indicator.LongSignalTriggered())
                    oneTrue= true;

            }

            return oneTrue;

        }

        private bool FirstIndicatorShortSignal()
        {
            bool oneTrue = false;
            foreach (var indicator in InnerIndicators)
            {
                if (indicator.ShortSignalTriggered())
                    oneTrue = true;

            }

            return oneTrue;

        }

        private bool CustomQtyLongSignal()
        {
            int customQty = 0;
            foreach (var indicator in InnerIndicators)
            {
                if (indicator.LongSignalTriggered())
                    customQty++;

            }


            return customQty >= OrchestationLogic.qtySignals;
        }

        private bool CustomQtyShortSignal()
        {
            int customQty = 0;
            foreach (var indicator in InnerIndicators)
            {
                if (indicator.ShortSignalTriggered())
                    customQty++;

            }


            return customQty >= OrchestationLogic.qtySignals;
        }

      
        #endregion

        #region Public Overriden Methods

        public void AppendIndicator(MonTurtlePosition newIndicator)
        {
            InnerIndicators.Add(newIndicator);
        
        }

        public override bool LongSignalTriggered() 
        {
            if (!EvalSkippingFalseLong())
                return false;//We will not open a position that will be inmediately closed

            if (OrchestationLogic.IsAllIndicators())
                return AllIndicatorsLongSignal();
            else if (OrchestationLogic.IsFirstSignal())
                return FirstIndicatorLongSignal();
            else if (OrchestationLogic.IsCustomQtySignals())
                return CustomQtyLongSignal();
            else
                throw new Exception($"CRITICAL error evaluating LONG signal for symbol {Security.Symbol}:no proper value for orchestation logic");
        }


        public override bool ShortSignalTriggered()
        {
            if (!EvalSkippingFalseShort())
                return false;//We will not open a position that will be inmediately closed

            if (OrchestationLogic.IsAllIndicators())
                return AllIndicatorsShortSignal();
            else if (OrchestationLogic.IsFirstSignal())
                return FirstIndicatorShortSignal();
            else if (OrchestationLogic.IsCustomQtySignals())
                return CustomQtyShortSignal();
            else
                throw new Exception($"CRITICAL error evaluating SHORT signal for symbol {Security.Symbol}:no proper value for orchestation logic");

        }

        public override string SignalTriggered()
        {
            string resp = $" Eval indicators for symbol {Security.Symbol}";


            foreach (var indicator in InnerIndicators)
            {
                resp += $" {indicator.SignalTriggered()} -";
            
            }

            return resp;
        }

        public override bool EvalClosingLongPosition(PortfolioPosition portfPos)
        {
            if (!portfPos.IsLongDirection())
                return false;

            if (EvalClosingOnTargetPct(portfPos))
                return true;

            if (CloseOnInnnerIndicators)
            {


                if (TurtlesCustomConfig.ExitOnMMov)
                {
                    foreach (var indicator in InnerIndicators)
                    {
                        if (!indicator.EvalClosingLongPosition(portfPos))
                            return false;

                    }

                    return true;

                }
                else if (TurtlesCustomConfig.ExitOnTurtles)
                {
                    return base.EvalClosingLongPosition(portfPos);

                }
                else
                {
                    throw new Exception($"No proper exit algo specified for symbol {Security.Symbol}");

                }
            }
            else
            {
                bool higherMMov = IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, true);
                DoLog($"DBG8- Eval Closing LONG signal CloseWindow={TurtlesCustomConfig.CloseWindow} higherMMov={higherMMov}", MessageType.Debug);
                return base.EvalClosingLongPosition(portfPos);
            }
        }

        public override bool EvalClosingShortPosition(PortfolioPosition portfPos)
        {
            DoLog($"DBG_SHORT_IMB_2 --> EvalClosingShortPosition: IsShortDirection{portfPos.IsShortDirection()} EvalClosingOnTargetPct={EvalClosingOnTargetPct(portfPos)}", zHFT.Main.Common.Util.Constants.MessageType.Information);
            if (!portfPos.IsShortDirection())
                return false;

            if (EvalClosingOnTargetPct(portfPos))
                return true;

            if (CloseOnInnnerIndicators)
            {

                if (TurtlesCustomConfig.ExitOnMMov)
                {
                    foreach (var indicator in InnerIndicators)
                    {
                        DoLog($"DBG_SHORT_IMB_2--> Evaluationg indicator {indicator.Security.Symbol}", zHFT.Main.Common.Util.Constants.MessageType.Information);
                        if (!indicator.EvalClosingShortPosition(portfPos))
                            return false;

                    }
                    DoLog($"DBG_SHORT_IMB_2--> Returning true as ALL the indicators were TRUE on EvalClosingShortPosition ", zHFT.Main.Common.Util.Constants.MessageType.Information);
                    return true;

                }
                else if (TurtlesCustomConfig.ExitOnTurtles)
                {
                    return base.EvalClosingShortPosition(portfPos);
                }
                else
                {
                    throw new Exception($"No proper exit algo specified for symbol {Security.Symbol}");

                }
            }
            else
            {
                bool higherMMov = IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, true);
                DoLog($"DBG8- Eval Closing SHORT signal CloseWindow={TurtlesCustomConfig.CloseWindow} higherMMov={higherMMov}", MessageType.Debug);
                return base.EvalClosingShortPosition(portfPos);
            }
        }

        public override string RelevantInnerInfo()
        {
            string resp = "";
            foreach (var indicator in InnerIndicators)
            {
                resp += $"Inner Info ofr ind {indicator.Security.Symbol}: {indicator.RelevantInnerInfo()}";

            }

            return resp;

        }



        public override List<MonitoringPosition> GetInnerIndicators()
        {

            List<MonitoringPosition> indicators = new List<MonitoringPosition>(InnerIndicators);

            return indicators;
        }




        #endregion


        #region IMonPosition

        public string GetCandleReferencePrice()
        {
            return CandleReferencePrice;
        }

        public int GetHistoricalPricesPeriod()
        {
            return HistoricalPricesPeriod;
        }

        public double GetStopLossForOpenPositionPct()
        {
            return StopLossForOpenPositionPct;
        }


        #endregion
    }
}
