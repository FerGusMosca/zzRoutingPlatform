using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;

namespace tph.ChainedTurtles.BusinessEntities
{
    public class MonChainedTrendlineTurtleIndicator: MonChainedTurtleIndicator
    {
        #region Constructor 

        public MonChainedTrendlineTurtleIndicator(Security pSecurity,
                                                  TurtlesCustomConfig pTurtlesCustomConfig,
                                                    string candleRefPrice,
                                                    string signalType,
                                                    bool reqMarketData):base(pSecurity,pTurtlesCustomConfig,candleRefPrice,signalType,reqMarketData)
        {

        }

        #endregion
    }
}
