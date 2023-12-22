using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;

namespace tph.ChainedTurtles.BusinessEntities
{
    public class MonChainedTurtleIndicator : MonTurtlePosition
    {

        #region Public Attributes

        public string SignalType { get; set; }

        public bool ReqPrices { get; set; }

        #endregion


        #region Constructor 

        public MonChainedTurtleIndicator(Security pSecurity, TurtlesCustomConfig pTurtlesCustomConfig,
                                        string candleRefPrice,string signalType,bool reqPrices) :base(pTurtlesCustomConfig, 0,candleRefPrice)
        {


            Security = pSecurity;
            SignalType = signalType;
            ReqPrices = reqPrices;


        }

        #endregion
    }
}
