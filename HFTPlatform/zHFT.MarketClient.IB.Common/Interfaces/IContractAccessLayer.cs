using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Interfaces;
using zHFT.MarketClient.IB.Common.Configuration;


namespace zHFT.MarketClient.IB.Common.Interfaces
{
    public interface IContractAccessLayer
    {
        IList<Contract> GetContracts(Configuration.Configuration Config,
                                     OnLogMessage pOnLogMsg);
    }
}
