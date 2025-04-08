using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.DTO.Position
{
    public class Cash
    {
        public double totalCash { get; set; }
        public Dictionary<string, double> detailedCash { get; set; }
    }
}
