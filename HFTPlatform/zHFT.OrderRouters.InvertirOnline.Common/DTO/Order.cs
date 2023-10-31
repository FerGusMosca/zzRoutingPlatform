using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.OrderRouters.InvertirOnline.Common.DTO
{
    public class Order : BaseOrder
    {
        #region Public Attributes

        #region zHFT Attributes

        public Side side { get; set; }

        public OrdType ordtype{ get; set; }

        public int OrderId { get; set; }

        public string ClOrdId{ get; set; }

        #endregion

        #endregion
    }
}
