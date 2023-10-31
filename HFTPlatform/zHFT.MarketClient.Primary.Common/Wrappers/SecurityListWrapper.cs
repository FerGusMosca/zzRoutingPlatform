using QuickFix;
using Shared.Bussiness.Fix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.MarketClient.Primary.Common.Wrappers
{
    public class SecurityListWrapper : Wrapper
    {
        #region Protected Attributes

        protected IConfiguration Config { get; set; }

        protected QuickFix50.SecurityList SecurityList { get; set; }

        #endregion

        #region Constructors

        public SecurityListWrapper(QuickFix50.SecurityList pSecurityList, IConfiguration pConfig) 
        {
            SecurityList = pSecurityList;

            Config = pConfig;
        }

        #endregion

        #region Private Mehods

        private List<Wrapper> GetSecurities()
        {
            List<Wrapper> securitiesWrappers = new List<Wrapper>();

            if (SecurityList.isSetField(NoRelatedSym.FIELD))
            {
                int numberOfEntries = SecurityList.getInt(NoRelatedSym.FIELD);

                for (int i = 1; i <= numberOfEntries; i++)
                {
                    QuickFix50.SecurityList.NoRelatedSym security = new QuickFix50.SecurityList.NoRelatedSym();
                    SecurityList.getGroup((uint)i, security);
                    securitiesWrappers.Add(new SecurityWrapper(security, Config));
                }
            }

            return securitiesWrappers;
        }

        private zHFT.Main.Common.Enums.SecurityRequestResult GetSecurityRequestResult(int result)
        {

            if (result == (int)zHFT.Main.Common.Enums.SecurityRequestResult.InstrumentDataTemprarilyUnavailable)
                return Main.Common.Enums.SecurityRequestResult.InstrumentDataTemprarilyUnavailable;
            else if (result == (int)zHFT.Main.Common.Enums.SecurityRequestResult.InvalidOrUnsupportedRequest)
                return Main.Common.Enums.SecurityRequestResult.InvalidOrUnsupportedRequest;
            else if (result == (int)zHFT.Main.Common.Enums.SecurityRequestResult.NoInstrumentsFoundForSelectionCriteria)
                return Main.Common.Enums.SecurityRequestResult.NoInstrumentsFoundForSelectionCriteria;
            else if (result == (int)zHFT.Main.Common.Enums.SecurityRequestResult.NotAuthorizedToRetrieve)
                return Main.Common.Enums.SecurityRequestResult.NotAuthorizedToRetrieve;
            else if (result == (int)zHFT.Main.Common.Enums.SecurityRequestResult.RequestPerInstrument)
                return Main.Common.Enums.SecurityRequestResult.RequestPerInstrument;
            else if (result == (int)zHFT.Main.Common.Enums.SecurityRequestResult.ValidRequest)
                return Main.Common.Enums.SecurityRequestResult.ValidRequest;
            else
                return Main.Common.Enums.SecurityRequestResult.Unknown;
        
        }

        private zHFT.Main.Common.Enums.SecurityListRequestType? GetSecurityListRequestType(int? result)
        {
            if (!result.HasValue)
                return null;

            int iresult = result.Value;

            if (iresult == (int)zHFT.Main.Common.Enums.SecurityListRequestType.AllSecurities)
                return Main.Common.Enums.SecurityListRequestType.AllSecurities;
            else if (iresult == (int)zHFT.Main.Common.Enums.SecurityListRequestType.MarketID)
                return Main.Common.Enums.SecurityListRequestType.MarketID;
            else if (iresult == (int)zHFT.Main.Common.Enums.SecurityListRequestType.Product)
                return Main.Common.Enums.SecurityListRequestType.Product;
            else if (iresult == (int)zHFT.Main.Common.Enums.SecurityListRequestType.SecurityType)
                return Main.Common.Enums.SecurityListRequestType.SecurityType;
            else if (iresult == (int)zHFT.Main.Common.Enums.SecurityListRequestType.Symbol)
                return Main.Common.Enums.SecurityListRequestType.Symbol;
            else if (iresult == (int)zHFT.Main.Common.Enums.SecurityListRequestType.TradingSessionID)
                return Main.Common.Enums.SecurityListRequestType.TradingSessionID;
            else
                return null;
        }


        #endregion


        #region Public Overriden Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            SecurityListFields slField = (SecurityListFields)field;

            if (SecurityList == null)
                return SecurityListFields.NULL;


            if (slField == SecurityListFields.SecurityRequestResult)
                return GetSecurityRequestResult(FixHelper.GetIntFieldIfSet(SecurityList, QuickFix.SecurityRequestResult.FIELD));
            else if (slField == SecurityListFields.SecurityListRequestType)
                return GetSecurityListRequestType(FixHelper.GetNullIntFieldIfSet(SecurityList, QuickFix.SecurityListRequestType.FIELD));
            else if (slField == SecurityListFields.MarketID)
                return FixHelper.GetNullFieldIfSet(SecurityList, QuickFix.MarketID.FIELD);
            else if (slField == SecurityListFields.MarketSegmentID)
                return FixHelper.GetNullFieldIfSet(SecurityList, QuickFix.MarketSegmentID.FIELD);
            else if (slField == SecurityListFields.TotNoRelatedSym)
                return FixHelper.GetNullIntFieldIfSet(SecurityList, QuickFix.TotNoRelatedSym.FIELD);
            else if (slField == SecurityListFields.LastFragment)
                return FixHelper.GetNullFieldIfSet(SecurityList, QuickFix.LastFragment.FIELD);
            else if (slField == SecurityListFields.Securities)
                return GetSecurities();

            return SecurityListFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.SECURITY_LIST;
        }

        #endregion
    }
}
