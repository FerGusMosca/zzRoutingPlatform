using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public class EconomicSeriesField : Fields
    {
        public static readonly EconomicSeriesField From = new EconomicSeriesField(2);
        public static readonly EconomicSeriesField To = new EconomicSeriesField(3);
        public static readonly EconomicSeriesField Values = new EconomicSeriesField(4);
        public static readonly EconomicSeriesField SeriesID = new EconomicSeriesField(5);
        protected EconomicSeriesField(int pInternalValue)
            : base(pInternalValue)
        {

        }
    }
}
