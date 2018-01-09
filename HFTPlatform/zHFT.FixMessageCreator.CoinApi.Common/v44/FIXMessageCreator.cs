using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Interfaces;

namespace zHFT.FixMessageCreator.CoinApi.Common.v44
{
    public class FIXMessageCreator : IFIXMessageCreator
    {
        public QuickFix.Message RequestMarketData(int id, string symbol, Main.Common.Enums.SubscriptionRequestType pSubscriptionRequestType)
        {
            throw new NotImplementedException();
        }

        public QuickFix.Message RequestSecurityList(int secType, string security)
        {
            throw new NotImplementedException();
        }

        public QuickFix.Message CreateNewOrderSingle(string clOrderId, string symbol, Main.Common.Enums.Side side, Main.Common.Enums.OrdType ordType, Main.Common.Enums.SettlType? settlType, Main.Common.Enums.TimeInForce? timeInForce, DateTime effectiveTime, double ordQty, double? price, double? stopPx, string account)
        {
            throw new NotImplementedException();
        }

        public QuickFix.Message CreateOrderCancelReplaceRequest(string clOrderId, string orderId, string origClOrdId, string symbol, Main.Common.Enums.Side side, Main.Common.Enums.OrdType ordType, Main.Common.Enums.SettlType? settlType, Main.Common.Enums.TimeInForce? timeInForce, DateTime effectiveTime, double? ordQty, double? price, double? stopPx, string account)
        {
            throw new NotImplementedException();
        }

        public QuickFix.Message CreateOrderCancelRequest(string clOrderId, string origClOrderId, string orderId, string symbol, Main.Common.Enums.Side side, DateTime effectiveTime, double? ordQty, string account, string mainExchange)
        {
            throw new NotImplementedException();
        }

        public void ProcessMarketData(QuickFix.Message snapshot, object security, OnLogMessage pOnLogMsg)
        {
            throw new NotImplementedException();
        }
    }
}
