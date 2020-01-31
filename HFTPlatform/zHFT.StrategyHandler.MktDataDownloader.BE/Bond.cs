using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.MktDataDownloader.BE
{
    public class Bond
    {
        public string Symbol { get; set; }

        public string Name { get; set; }

        public string Market { get; set; }

        public string Country { get; set; }
    }
}
