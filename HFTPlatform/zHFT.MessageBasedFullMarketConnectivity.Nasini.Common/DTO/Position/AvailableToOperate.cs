using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.DTO.Position
{
    public class AvailableToOperate
    {
        public Cash cash { get; set; }
        public double movements { get; set; }
        public double? credit { get; set; }
        public double total { get; set; }
        public double pendingMovements { get; set; }
    }
}
