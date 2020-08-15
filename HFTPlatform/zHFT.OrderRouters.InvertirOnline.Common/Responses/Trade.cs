using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.OrderRouters.InvertirOnline.Common.Responses
{
    public class Trade
    {
        #region Public Attributes

        public DateTime? fecha { get; set; }

        public int cantidad { get; set; }

        public double precio { get; set; }

        #endregion
    }
}
