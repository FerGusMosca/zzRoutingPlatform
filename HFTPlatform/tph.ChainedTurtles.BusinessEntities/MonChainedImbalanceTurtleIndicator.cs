using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;

namespace tph.ChainedTurtles.BusinessEntities
{
    public class MonChainedImbalanceTurtleIndicator : MonChainedTurtleIndicator
    {

        #region Constructors

        public MonChainedImbalanceTurtleIndicator(Security pSecurity, TurtlesCustomConfig pTurtlesCustomConfig, string candleRefPrice, string pCode, string signalType, bool reqPrices) : base(pSecurity, pTurtlesCustomConfig, candleRefPrice, pCode, signalType, reqPrices)
        {
        }


        #endregion


        #region Base Overriden Methods


        public override bool EvalSignalTriggered()
        {
            bool longSignal = LongSignalTriggered();
            bool shortSignal = ShortSignalTriggered();

            return longSignal || shortSignal;

        }


        public override bool LongSignalTriggered()
        {
            //TODO--> Long imbalance activated?
            return false;
        
        }


        public override bool ShortSignalTriggered()
        {
            //TODO --> Short imbalance activated?
            return false;

        }


        //EvalClosingShortPosition --> Uses standard closing mechanism
        //EvalClosingLongPosition -->  Uses standard closing mehcanism


        #endregion
    }
}
