using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouters.Bitmex.Common.Wrappers
{
    public  class BaseExecutionReportWrapper:Wrapper
    {

        #region Protected Methods

        protected ExecType GetExecTypeFromBitMexStatus(string execType)
        {
            if (execType == ExecType.Calculated.ToString())
                return ExecType.Calculated;
            else if (execType == ExecType.Canceled.ToString())
                return ExecType.Canceled;
            else if (execType == ExecType.DoneForDay.ToString())
                return ExecType.DoneForDay;
            else if (execType == ExecType.Expired.ToString())
                return ExecType.Expired;
            else if (execType == ExecType.New.ToString())
                return ExecType.New;
            else if (execType == ExecType.OrderStatus.ToString())
                return ExecType.OrderStatus;
            else if (execType == ExecType.PendingCancel.ToString())
                return ExecType.PendingCancel;
            else if (execType == ExecType.PendingNew.ToString())
                return ExecType.PendingNew;
            else if (execType == ExecType.PendingReplace.ToString())
                return ExecType.PendingReplace;
            else if (execType == ExecType.Rejected.ToString())
                return ExecType.Rejected;
            else if (execType == ExecType.Replaced.ToString())
                return ExecType.Replaced;
            else if (execType == ExecType.Restated.ToString())
                return ExecType.Restated;
            else if (execType == ExecType.Stopped.ToString())
                return ExecType.Stopped;
            else if (execType == ExecType.Suspended.ToString())
                return ExecType.Suspended;
            else if (execType == ExecType.Trade.ToString())
                return ExecType.Trade;
            else if (execType == ExecType.TradeCancel.ToString())
                return ExecType.TradeCancel;
            else if (execType == ExecType.TradeCorrect.ToString())
                return ExecType.TradeCorrect;
            else if (execType == ExecType.Unknown.ToString())
                return ExecType.Unknown;
            else
                throw new Exception(string.Format("Unknown exec type for execution report:{0}", execType));
        }

        protected OrdStatus GetOrdStatusFromBitMexStatus(string ordStatus)
        {

            if (ordStatus == OrdStatus.AcceptedForBidding.ToString())
                return OrdStatus.AcceptedForBidding;
            else if (ordStatus == OrdStatus.Calculated.ToString())
                return OrdStatus.Calculated;
            else if (ordStatus == OrdStatus.Canceled.ToString())
                return OrdStatus.Canceled;
            else if (ordStatus == OrdStatus.DoneForDay.ToString())
                return OrdStatus.DoneForDay;
            else if (ordStatus == OrdStatus.Expired.ToString())
                return OrdStatus.Expired;
            else if (ordStatus == OrdStatus.Filled.ToString())
                return OrdStatus.Filled;
            else if (ordStatus == OrdStatus.New.ToString())
                return OrdStatus.New;
            else if (ordStatus == OrdStatus.PartiallyFilled.ToString())
                return OrdStatus.PartiallyFilled;
            else if (ordStatus == OrdStatus.PendingCancel.ToString())
                return OrdStatus.PendingCancel;
            else if (ordStatus == OrdStatus.PendingNew.ToString())
                return OrdStatus.PendingNew;
            else if (ordStatus == OrdStatus.PendingReplace.ToString())
                return OrdStatus.PendingReplace;
            else if (ordStatus == OrdStatus.Rejected.ToString())
                return OrdStatus.Rejected;
            else if (ordStatus == OrdStatus.Replaced.ToString())
                return OrdStatus.Replaced;
            else if (ordStatus == OrdStatus.Stopped.ToString())
                return OrdStatus.Stopped;
            else if (ordStatus == OrdStatus.Suspended.ToString())
                return OrdStatus.Suspended;
            else
                throw new Exception(string.Format("Unknown ord status for execution report:{0}", ordStatus));

        }

        protected OrdRejReason GetOrdRejReasonFromBitMexStatus(string ordRejReason)
        {

            if (ordRejReason == OrdRejReason.Broker.ToString())
                return OrdRejReason.Broker;
            else if (ordRejReason == OrdRejReason.DuplicateAVerballyCommunicatedOrder.ToString())
                return OrdRejReason.DuplicateAVerballyCommunicatedOrder;
            else if (ordRejReason == OrdRejReason.DuplicateOrder.ToString())
                return OrdRejReason.DuplicateOrder;
            else if (ordRejReason == OrdRejReason.ExchangeClosed.ToString())
                return OrdRejReason.ExchangeClosed;
            else if (ordRejReason == OrdRejReason.IncorrectAllocatedQuantity.ToString())
                return OrdRejReason.IncorrectAllocatedQuantity;
            else if (ordRejReason == OrdRejReason.IncorrectQuantity.ToString())
                return OrdRejReason.IncorrectQuantity;
            else if (ordRejReason == OrdRejReason.InvalidInvestorID.ToString())
                return OrdRejReason.InvalidInvestorID;
            else if (ordRejReason == OrdRejReason.InvalidPriceIncrement.ToString())
                return OrdRejReason.InvalidPriceIncrement;
            else if (ordRejReason == OrdRejReason.OrderExceedsLimit.ToString())
                return OrdRejReason.OrderExceedsLimit;
            else if (ordRejReason == OrdRejReason.Other.ToString())
                return OrdRejReason.Other;
            else if (ordRejReason == OrdRejReason.StaleOrder.ToString())
                return OrdRejReason.StaleOrder;
            else if (ordRejReason == OrdRejReason.SurveillanceOption.ToString())
                return OrdRejReason.SurveillanceOption;
            else if (ordRejReason == OrdRejReason.TooLateToEnter.ToString())
                return OrdRejReason.TooLateToEnter;
            else if (ordRejReason == OrdRejReason.TradeAlongRequired.ToString())
                return OrdRejReason.TradeAlongRequired;
            else if (ordRejReason == OrdRejReason.UnknownOrder.ToString())
                return OrdRejReason.UnknownOrder;
            else if (ordRejReason == OrdRejReason.UnknownSymbol.ToString())
                return OrdRejReason.UnknownSymbol;
            else if (ordRejReason == OrdRejReason.UnkwnownAccount.ToString())
                return OrdRejReason.UnkwnownAccount;
            else if (ordRejReason == OrdRejReason.UnsupportedOrderCharacteristic.ToString())
                return OrdRejReason.UnsupportedOrderCharacteristic;
            else
                return OrdRejReason.Other;


        }


        #endregion

        #region Abstract Methods

        public override object GetField(Main.Common.Enums.Fields field) {

            return ExecutionReportFields.NULL;
        
        }

        public override Main.Common.Enums.Actions GetAction()
        {

            return Actions.EXECUTION_REPORT;
        }

        #endregion
    }
}
