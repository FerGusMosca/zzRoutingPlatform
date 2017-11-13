using QuickFix;
using Shared.Bussiness.Fix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouters.Primary.Common.Wrappers
{
    public class OrderCancelRejectWrapper:Wrapper
    {
        #region Private Attributes

        protected QuickFix50.OrderCancelReject OrderCancelReject { get; set; }

        #endregion

        #region Constructors

        public OrderCancelRejectWrapper(QuickFix50.OrderCancelReject pOrderCancelReject)
        {
            OrderCancelReject = pOrderCancelReject;
        }

        #endregion

        #region Protected Methods

        protected zHFT.Main.Common.Enums.OrdStatus GetOrdStatus(char state)
        {
            if (state == QuickFix.OrdStatus.PENDING_NEW)
                return zHFT.Main.Common.Enums.OrdStatus.PendingNew;
            else if (state == QuickFix.OrdStatus.NEW)
                return zHFT.Main.Common.Enums.OrdStatus.New;
            else if (state == QuickFix.OrdStatus.REJECTED)
                return zHFT.Main.Common.Enums.OrdStatus.Rejected;
            else if (state == QuickFix.OrdStatus.CANCELED)
                return zHFT.Main.Common.Enums.OrdStatus.Canceled;
            else if (state == QuickFix.OrdStatus.EXPIRED)
                return zHFT.Main.Common.Enums.OrdStatus.Expired;
            else if (state == QuickFix.OrdStatus.PENDING_REPLACE)
                return zHFT.Main.Common.Enums.OrdStatus.PendingReplace;
            else if (state == QuickFix.OrdStatus.REPLACED)
                return zHFT.Main.Common.Enums.OrdStatus.Replaced;
            else if (state == QuickFix.OrdStatus.FILLED)
                return zHFT.Main.Common.Enums.OrdStatus.Filled;
            else if (state == QuickFix.OrdStatus.PARTIALLY_FILLED)
                return zHFT.Main.Common.Enums.OrdStatus.PartiallyFilled;
            else
                throw new Exception("OrdStatus not supported: " + state.ToString());
        }

        protected zHFT.Main.Common.Enums.CxlRejResponseTo GetCxlRejResponseTo(char respTo)
        {
            if (respTo == QuickFix.CxlRejResponseTo.ORDER_CANCEL_REQUEST)
                return zHFT.Main.Common.Enums.CxlRejResponseTo.OrderCancelRequest;
            else if (respTo == QuickFix.CxlRejResponseTo.ORDER_CANCEL_REPLACE_REQUEST)
                return zHFT.Main.Common.Enums.CxlRejResponseTo.OrderCancelReplaceRequest;
            else
                throw new Exception("CxlRejResponseTo not supported: " + respTo.ToString());
        
        }

        protected zHFT.Main.Common.Enums.CxlRejReason GetCxlRejReason(char rejReason)
        {
            if (rejReason == QuickFix.CxlRejReason.ALREADYPENDINGCXL)
                return zHFT.Main.Common.Enums.CxlRejReason.OrderAlreadyPendingCancelOrPendingReplace;
            else if (rejReason == QuickFix.CxlRejReason.BROKER)
                return zHFT.Main.Common.Enums.CxlRejReason.BrokerExchangeOption;
            else if (rejReason == QuickFix.CxlRejReason.INVALID_PRICE_INCREMENT)
                return zHFT.Main.Common.Enums.CxlRejReason.InvalidPriceIncrement;
            else if (rejReason == QuickFix.CxlRejReason.DUPLICATE_CLORDID)
                return zHFT.Main.Common.Enums.CxlRejReason.DuplicateCLOrdId;
            else if (rejReason == QuickFix.CxlRejReason.ORIGORDMODTIME_DID_NOT_MATCH_LAST_TRANSACTTIME_OF_ORDER)
                return zHFT.Main.Common.Enums.CxlRejReason.OrigOrdModTimeDidNotMatchLastTransactTime;
            else if (rejReason == QuickFix.CxlRejReason.OTHER)
                return zHFT.Main.Common.Enums.CxlRejReason.Other;
            else if (rejReason == QuickFix.CxlRejReason.PRICE_EXCEEDS_CURRENT_PRICE)
                return zHFT.Main.Common.Enums.CxlRejReason.PriceExceedsCurrentPrice;
            else if (rejReason == QuickFix.CxlRejReason.PRICE_EXCEEDS_CURRENT_PRICE_BAND)
                return zHFT.Main.Common.Enums.CxlRejReason.PriceExceedsCurrentPriceBand;
            else if (rejReason == QuickFix.CxlRejReason.TOO_LATE_TO_CANCEL)
                return zHFT.Main.Common.Enums.CxlRejReason.TooLateToCancel;
            else if (rejReason == QuickFix.CxlRejReason.UNABLE_TO_PROCESS_ORDER_MASS_CANCEL_REQUEST)
                return zHFT.Main.Common.Enums.CxlRejReason.UnableProcessOrderMassCancelRequest;
            else if (rejReason == QuickFix.CxlRejReason.UNKNOWN_ORDER)
                return zHFT.Main.Common.Enums.CxlRejReason.UnknownOrder;
            else
                throw new Exception("CxlRejReason not supported: " + rejReason.ToString());

        }


        #endregion


        #region Public Methods

        public override object GetField(Fields field)
        {
            OrderCancelRejectField ocrField = (OrderCancelRejectField)field;

            if (OrderCancelReject == null)
                return OrderCancelRejectField.NULL;

            if (ocrField == OrderCancelRejectField.OrderID)
                return FixHelper.GetFieldIfSet(OrderCancelReject, OrderID.FIELD);
            else if (ocrField == OrderCancelRejectField.ClOrdID)
                return FixHelper.GetFieldIfSet(OrderCancelReject, ClOrdID.FIELD);
            else if (ocrField == OrderCancelRejectField.OrigClOrdID)
                return FixHelper.GetFieldIfSet(OrderCancelReject, OrigClOrdID.FIELD);
            else if (ocrField == OrderCancelRejectField.Text)
                return FixHelper.GetFieldIfSet(OrderCancelReject, Text.FIELD);
            else if (ocrField == OrderCancelRejectField.OrdStatus)
                return GetOrdStatus(OrderCancelReject.getChar(QuickFix.OrdStatus.FIELD));
            else if (ocrField == OrderCancelRejectField.CxlRejResponseTo)
                return GetCxlRejResponseTo(OrderCancelReject.getChar(QuickFix.CxlRejResponseTo.FIELD));
            else if (ocrField == OrderCancelRejectField.CxlRejReason)
                return GetCxlRejReason(OrderCancelReject.getChar(QuickFix.CxlRejReason.FIELD));
            else
                return OrderCancelRejectField.NULL; 
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.ORDER_CANCEL_REJECT;
        }

        #endregion
    }
}
