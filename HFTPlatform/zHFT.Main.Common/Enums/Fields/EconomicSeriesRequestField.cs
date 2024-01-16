using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public class EconomicSeriesRequestField : Fields
    {
        public static readonly EconomicSeriesRequestField From = new EconomicSeriesRequestField(3);
        public static readonly EconomicSeriesRequestField To = new EconomicSeriesRequestField(4);
        public static readonly EconomicSeriesRequestField SeriesID = new EconomicSeriesRequestField(5);
        public static readonly EconomicSeriesRequestField Interval = new EconomicSeriesRequestField(6);

        protected EconomicSeriesRequestField(int pInternalValue)
            : base(pInternalValue)
        {

        }
    }
}
