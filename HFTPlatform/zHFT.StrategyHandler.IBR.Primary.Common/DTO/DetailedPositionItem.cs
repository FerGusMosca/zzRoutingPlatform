using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.IBR.Primary.Common.DTO
{
    public class DetailedPositionItem
    {
        public string currency { get; set; }

        public string symbolReference { get; set; }

        public decimal? exchangeRate { get; set; }

        public string contractType { get; set; }

        public decimal? contractSize { get; set; }

        public decimal? priceConversionFactor { get; set; }

        public decimal? totalInitialSize { get; set; }

        public decimal? totalFilledSize { get; set; }

        public decimal? buyInitialPrice { get; set; }

        public decimal? buyInitialSize { get; set; }

        public decimal? buyFilledPrice { get; set; }

        public decimal? buyFilledSize { get; set; }

        public decimal? sellInitialPrice { get; set; }

        public decimal? sellInitialSize { get; set; }

        public decimal? sellFilledPrice { get; set; }

        public decimal? sellFilledSize { get; set; }

        public decimal? marketPrice { get; set; }

        public decimal? buyCurrentSize { get; set; }

        public decimal? sellCurrentSize { get; set; }

        public decimal? totalCurrentSize { get; set; }

        public DetailedDailyDiff detailedDailyDiff { get; set; }


    }
}
