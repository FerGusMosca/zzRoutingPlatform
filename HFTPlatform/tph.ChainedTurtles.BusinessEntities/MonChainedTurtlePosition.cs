using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;

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



        #endregion
    }
}
