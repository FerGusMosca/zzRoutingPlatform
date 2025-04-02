using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public class PortfolioRequestFields : Fields
    {
        public static readonly PortfolioRequestFields AccountNumber = new PortfolioRequestFields(2);

        protected PortfolioRequestFields(int pInternalValue)
         : base(pInternalValue)
        {

        }
    }
}
