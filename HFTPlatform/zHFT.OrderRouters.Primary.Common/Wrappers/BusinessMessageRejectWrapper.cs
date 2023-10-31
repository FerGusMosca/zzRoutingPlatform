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
    public class BusinessMessageRejectWrapper : Wrapper
    {
        #region Private Attributes

        protected QuickFix50.BusinessMessageReject BusinessMessageReject { get; set; }

        #endregion

        #region Constructors

        public BusinessMessageRejectWrapper(QuickFix50.BusinessMessageReject pBusinessMessageReject)
        {
            BusinessMessageReject = pBusinessMessageReject;
        }

        #endregion

        #region Protected Methods

        protected zHFT.Main.Common.Enums.BusinessRejectReason GetBusinessRejectReason(int reason)
        {
            if (reason == QuickFix.BusinessRejectReason.APPLICATION_NOT_AVAILABLE)
                return zHFT.Main.Common.Enums.BusinessRejectReason.ApplicationNotAvailable;
            else if (reason == QuickFix.BusinessRejectReason.CONDITIONALLY_REQUIRED_FIELD_MISSING)
                return zHFT.Main.Common.Enums.BusinessRejectReason.ConditionallyRequiredFieldMissing;
            else if (reason == QuickFix.BusinessRejectReason.OTHER)
                return zHFT.Main.Common.Enums.BusinessRejectReason.Other;
            else if (reason == QuickFix.BusinessRejectReason.UNKNOWN_ID)
                return zHFT.Main.Common.Enums.BusinessRejectReason.UnknownID;
            else if (reason == QuickFix.BusinessRejectReason.UNKNOWN_SECURITY)
                return zHFT.Main.Common.Enums.BusinessRejectReason.UnkwownSecurity;
            else if (reason == QuickFix.BusinessRejectReason.UNSUPPORTED_MESSAGE_TYPE)
                return zHFT.Main.Common.Enums.BusinessRejectReason.UnsupportedMessageType;
            else
                throw new Exception(string.Format("Unknown business reject reason {0}", reason));
        }


        #endregion

        #region Public Methods

        public override object GetField(Fields field)
        {
            BusinessMessageRejectField bmrField = (BusinessMessageRejectField)field;

            if (BusinessMessageReject == null)
                return BusinessMessageRejectField.NULL;

            if (bmrField == BusinessMessageRejectField.RefMsgType)
                return BusinessMessageReject.getField(RefMsgType.FIELD);
            else if (bmrField == BusinessMessageRejectField.BusinessRejectRefID)
                return FixHelper.GetFieldIfSet(BusinessMessageReject, BusinessRejectRefID.FIELD);
            else if (bmrField == BusinessMessageRejectField.Text)
                return FixHelper.GetFieldIfSet(BusinessMessageReject, Text.FIELD);
            else if (bmrField == BusinessMessageRejectField.BusinessRejectReason)
                return GetBusinessRejectReason(FixHelper.GetIntFieldIfSet(BusinessMessageReject, QuickFix.BusinessRejectReason.FIELD));
            else
                return OrderCancelRejectField.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.BUSINESS_MESSAGE_REJECT;
        }


        #endregion
    }
}
