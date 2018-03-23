using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;

namespace zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces
{
    public interface IMarketDataReferenceHandler
    {
        MarketData GetMarketData(string symbol);
    }
}
