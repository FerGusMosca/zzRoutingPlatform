using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Interfaces
{
    public interface IFIXMessageCreator
    {
        QuickFix.Message RequestMarketData(int id, string symbol);

        QuickFix.Message RequestSecurityList(int secType, string security);

        QuickFix.Message CreateNewOrderSingle(string clOrderId, string symbol,
                                             zHFT.Main.Common.Enums.Side side,
                                             zHFT.Main.Common.Enums.OrdType ordType,
                                             zHFT.Main.Common.Enums.SettlType? settlType,
                                             zHFT.Main.Common.Enums.TimeInForce? timeInForce,
                                             double ordQty, double? price, double? stopPx, string account);

        void ProcessMarketData(QuickFix.Message snapshot, object security, OnLogMessage pOnLogMsg);
    }
}
