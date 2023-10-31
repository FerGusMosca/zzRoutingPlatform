using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouters.InvertirOnline.Common.Wrappers
{
    public class OrderCancelRejectWrapper : zHFT.OrderRouters.Common.Wrappers.OrderCancelRejectWrapper
    {
        public OrderCancelRejectWrapper(string pClOrdId, string pOrderId, CxlRejResponseTo pCxlRejResponseTo, CxlRejReason pCxlRejReason, string pReason) : base(pClOrdId, pOrderId, pReason, pCxlRejReason, pCxlRejResponseTo)
        {
            
        }
    }
}
