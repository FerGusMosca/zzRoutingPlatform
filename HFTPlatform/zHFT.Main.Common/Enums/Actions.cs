using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public enum Actions
    {
        MARKET_DATA,
        MARKET_DATA_REQUEST,
        ORDER_BOOK,
        OFFER,
        EXECUTION_REPORT,
        NEW_POSITION,
        SECURITY,
        NEW_POSITION_CANCELED,
        NEW_ORDER,
        UPDATE_ORDER,
        CANCEL_ORDER,
        CANCEL_POSITION,
        SECURITY_LIST
    }
}
