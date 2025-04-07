using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.Position;

namespace zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.Generic
{
    public class GetPositionsResp:GenericResponse
    {
        public string status { get; set; }
        public List<PortfolioPosition> positions { get; set; }
    }
}
