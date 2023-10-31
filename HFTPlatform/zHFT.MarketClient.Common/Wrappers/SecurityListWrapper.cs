using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.MarketClient.Common.Wrappers
{
    public class SecurityListWrapper : Wrapper
    {
        #region Private Static Consts

        private static string _MARKET_ID = "";

        #endregion

        #region Protected Attributes



        protected List<Wrapper> Securities { get; set; }

        protected SecurityListRequestType SecurityListRequestType { get; set; } 


        protected string MarketId { get; set; }

        protected int SecListReqId { get; set; }

        #endregion

        #region Constructors

        public SecurityListWrapper(int pSecListReqId,List<Wrapper> pSecurities, SecurityListRequestType pSecurityListRequestType,
                                    string pMarketId)
        {
            Securities = pSecurities;
            SecurityListRequestType = pSecurityListRequestType;

            MarketId = pMarketId;

            SecListReqId = pSecListReqId;
        }

        #endregion

        #region Protected Methods


        #endregion

        #region Public Mehods

        public override object GetField(Fields field)
        {
            SecurityListFields slField = (SecurityListFields)field;

            if (slField == SecurityListFields.SecurityRequestResult)
                return SecurityRequestResult.ValidRequest;
            else if (slField == SecurityListFields.SecurityListRequestType)
                return SecurityListRequestType;
            else if (slField == SecurityListFields.MarketID)
                return MarketId;
            else if (slField == SecurityListFields.MarketSegmentID)
                return null;
            else if (slField == SecurityListFields.TotNoRelatedSym)
                return Securities != null ? Securities.Count() : 0;
            else if (slField == SecurityListFields.LastFragment)
                return true;
            else if (slField == SecurityListFields.Securities)
                return Securities;
            else if (slField == SecurityListFields.SecurityListRequestId)
                return SecListReqId;

            //

            return SecurityListFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.SECURITY_LIST;
        }

        #endregion

    }
}
