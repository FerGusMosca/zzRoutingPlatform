using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;
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
                                        string candleRefPrice):base(pTurtlesCustomConfig,stopLossForOpenPositionPct,candleRefPrice)
        {

            Security = pSecurity;

            InnerIndicators = new List<MonTurtlePosition>();





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

        public override List<MonitoringPosition> GetInnerIndicators()
        {

            List<MonitoringPosition> indicators = new List<MonitoringPosition>(InnerIndicators);

            return indicators;
        }


        #endregion
    }
}
