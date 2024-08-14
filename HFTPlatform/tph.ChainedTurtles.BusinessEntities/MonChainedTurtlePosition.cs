using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.DTO;
using tph.ChainedTurtles.Common.Interfaces;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Util;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.ChainedTurtles.BusinessEntities
{
    public class MonChainedTurtlePosition: MonTrendlineTurtlesPosition, IMonPosition
    {

        #region Protected Attributes

        protected List<MonTurtlePosition> InnerIndicators { get; set; }

        #endregion

        #region Constructor 

        public MonChainedTurtlePosition(Security pSecurity,TurtlesCustomConfig pTurtlesCustomConfig, 
                                        MonitoringType pMonitoringType):base(pTurtlesCustomConfig,0,null)
        {

            Security = pSecurity;

            InnerIndicators = new List<MonTurtlePosition>();

            LoadConfigValues(pTurtlesCustomConfig.CustomConfig);//This is where Stop Loss and CandleRef price are loaded

            MonitoringType = pMonitoringType;
        }

        #endregion

        #region Protected Attributes

        protected int HistoricalPricesPeriod { get; set; }

        protected double StopLossForOpenPositionPct { get; set; }

        #endregion


        #region Private Methods

        private void LoadConfigValues(string customConfig)
        {
            //
            try
            {
                MonPosTrutleIndicatorConfigDTO resp = JsonConvert.DeserializeObject<MonPosTrutleIndicatorConfigDTO>(customConfig);


                if (!string.IsNullOrEmpty(resp.marketStartTime))
                {
                    EvalTime(resp.marketStartTime);
                    MarketStartTime = resp.marketStartTime;
                }
                else
                    throw new Exception("Missing config value marketStartTime");

                if (!string.IsNullOrEmpty(resp.marketEndTime))
                {
                    EvalTime(resp.marketEndTime);
                    MarketEndTime = resp.marketEndTime;
                }
                else
                    throw new Exception("Missing config value marketEndTime");


                if (!string.IsNullOrEmpty(resp.closingTime))
                {
                    EvalTime(resp.closingTime);
                    ClosingTime = resp.closingTime;
                }
                else
                    throw new Exception("Missing config value closingTime");



                if (!string.IsNullOrEmpty(resp.candleReferencePrice))
                {

                    CandleReferencePrice = resp.candleReferencePrice;
                }
                else
                    throw new Exception("Missing config value candleReferencePrice");


                if (resp.historicalPricesPeriod <= 0)
                    HistoricalPricesPeriod = resp.historicalPricesPeriod;
                else
                    throw new Exception("config value historicalPricesPeriod must be lower than 0");

                if (resp.stopLossForOpenPositionPct >= 0)
                    StopLossForOpenPositionPct = resp.stopLossForOpenPositionPct;
                else
                    throw new Exception("config value stopLossForOpenPositionPct must be greater or equal than 0");


            }
            catch (Exception ex)
            {
                throw new Exception($"CRITICAL error deserializing custom config for symbol {Security.Symbol}:{ex.Message} ");
            }
        }

        #endregion

        #region Public Overriden Methods

        public void AppendIndicator(MonTurtlePosition newIndicator)
        {
            InnerIndicators.Add(newIndicator);
        
        }

        public override bool LongSignalTriggered() 
        {
            
            foreach (var indicator in InnerIndicators)
            {
                if (!indicator.LongSignalTriggered())
                    return false;
            
            }

            return InnerIndicators.Count>0;    
        }


        public override bool ShortSignalTriggered()
        {

            foreach (var indicator in InnerIndicators)
            {
                if (!indicator.ShortSignalTriggered())
                    return false;

            }

            return InnerIndicators.Count > 0;
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

        public override bool EvalClosingShortPosition(PortfolioPosition portfPos)
        {
            if (!portfPos.IsShortDirection())
                return false;

            if (EvalClosingOnTargetPct(portfPos))
                return true;

            if (TurtlesCustomConfig.ExitOnMMov)
            {
                foreach (var indicator in InnerIndicators)
                {
                    if (!indicator.EvalClosingShortPosition(portfPos))
                        return false;

                }

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
