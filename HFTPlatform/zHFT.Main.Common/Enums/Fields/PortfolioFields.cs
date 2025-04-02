using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public class PortfolioFields : Fields
    {
        public static readonly PortfolioFields SecurityPositions = new PortfolioFields(2);
        public static readonly PortfolioFields LiquidPositions = new PortfolioFields(3);
        public static readonly PortfolioFields AccountNumber = new PortfolioFields(4);

        protected PortfolioFields(int pInternalValue)
         : base(pInternalValue)
        {

        }
    }
}
