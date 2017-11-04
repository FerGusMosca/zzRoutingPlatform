using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.OptionsMarketClient.BusinessEntities;

namespace zHFT.OptionsMarketClient.Common.Interfaces
{
    public interface IContractFilter
    {
        List<Option> FilterContracts(Security security,
                                       List<Option> options,
                                       OptionsMarketClient.Common.Configuration.Configuration config);

        bool ValidContract(Option option, OptionsMarketClient.Common.Configuration.Configuration config);
    }
}
