using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.Interfaces;

namespace tph.ChainedTurtles.Common
{
    public interface ITrendlineIndicator: ITradingEnity
    {

        #region Methods


        int GetInnerTrendlinesSpan();
        int GetOutterTrendlinesSpan();
        
        double GetPerforationThreshold ();

        bool GetRecalculateTrendlines();

        

        int GetSkipCandlesToBreakTrndln();

        #endregion


    }
}
