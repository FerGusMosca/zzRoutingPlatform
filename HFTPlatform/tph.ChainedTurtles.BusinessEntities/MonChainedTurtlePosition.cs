using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Util;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.ChainedTurtles.BusinessEntities
{
    public class MonChainedTurtlePosition: MonTrendlineTurtlesPosition
    {

        #region Protected Attributes

        protected List<MonTurtlePosition> InnerIndicators { get; set; }

        #endregion

        #region Constructor 

        public MonChainedTurtlePosition(Security pSecurity,TurtlesCustomConfig pTurtlesCustomConfig, 
                                        double stopLossForOpenPositionPct, 
                                        string candleRefPrice, 
                                        MonitoringType pMonitoringType):base(pTurtlesCustomConfig,stopLossForOpenPositionPct,candleRefPrice)
        {

            Security = pSecurity;

            InnerIndicators = new List<MonTurtlePosition>();


            MonitoringType = pMonitoringType;
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
    }
}
