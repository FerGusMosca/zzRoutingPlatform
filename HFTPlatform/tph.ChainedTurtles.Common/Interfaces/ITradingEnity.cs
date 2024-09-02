using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;

namespace tph.ChainedTurtles.Common.Interfaces
{
    public interface ITradingEnity
    {
        string GetCandleReferencePrice();

        int GetHistoricalPricesPeriod();


        List<Security> GetSecurities();
    }
}
