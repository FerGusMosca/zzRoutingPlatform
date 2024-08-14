using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.Interfaces
{
    public interface ITradingEnity
    {
        string GetCandleReferencePrice();

        int GetHistoricalPricesPeriod();
    }
}
