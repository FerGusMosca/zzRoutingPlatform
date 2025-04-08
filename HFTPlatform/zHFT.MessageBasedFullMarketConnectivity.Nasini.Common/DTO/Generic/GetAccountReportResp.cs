using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.DTO.Position;

namespace zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.DTO.Generic
{
    public class GetAccountReportResp : GenericResponse
    {
        public string status { get; set; }
        public AccountData accountData { get; set; }
    }

}
