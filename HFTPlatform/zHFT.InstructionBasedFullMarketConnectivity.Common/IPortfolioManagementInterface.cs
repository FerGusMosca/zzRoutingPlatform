using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;

namespace zHFT.InstructionBasedFullMarketConnectivity.Common
{
    public interface IPortfolioManagementInterface
    {
        GenericResponse Authenticate();

        GenericResponse GetPortfolio(string accNumber);
    }
}
