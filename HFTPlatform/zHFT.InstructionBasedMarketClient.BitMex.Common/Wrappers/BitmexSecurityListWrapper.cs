using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BitMex.BE;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.Wrappers
{
    public class BitmexSecurityListWrapper:Wrapper
    {
          #region Protected Attributes

        protected IConfiguration Config { get; set; }

        protected List<Security> SecurityList { get; set; }

        #endregion

        #region Constructors

        public BitmexSecurityListWrapper(List<Security> pSecurityList, IConfiguration pConfig) 
        {
            SecurityList = pSecurityList;

            Config = pConfig;
        }

        #endregion

        #region Private Mehods

        private List<Wrapper> GetSecurities()
        {
            List<Wrapper> securitiesWrappers = new List<Wrapper>();

            foreach(Security sec in SecurityList)
            {
                securitiesWrappers.Add(new BitMexSecurityWrapper(sec,Config));
            }

            return securitiesWrappers;
        }

        #endregion


        #region Public Overriden Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            SecurityListFields slField = (SecurityListFields)field;

            if (SecurityList == null)
                return SecurityListFields.NULL;


            if (slField == SecurityListFields.SecurityRequestResult)
                return Main.Common.Enums.SecurityRequestResult.ValidRequest;
            else if (slField == SecurityListFields.SecurityListRequestType)
                return Main.Common.Enums.SecurityListRequestType.AllSecurities;
            else if (slField == SecurityListFields.MarketID)
                return "BitMex";
            else if (slField == SecurityListFields.MarketSegmentID)
                return SecurityListFields.NULL;
            else if (slField == SecurityListFields.TotNoRelatedSym)
                return SecurityList.Count;
            else if (slField == SecurityListFields.LastFragment)
                return "yes";
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
