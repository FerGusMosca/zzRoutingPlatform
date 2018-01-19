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
        CANCEL_ALL_POSITIONS,
        SECURITY_LIST,
        SECURITY_LIST_REQUEST,
        ORDER_CANCEL_REJECT,
        BUSINESS_MESSAGE_REJECT,
        ORDER_LIST
    }
}
