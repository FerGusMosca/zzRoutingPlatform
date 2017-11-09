using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.BusinessEntities.Security_List;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.StrategyHandler.Common.Converters
{
    public class SecurityListConverter : ConverterBase
    {
        #region Private Methods

        protected void ValidateSecurityList(Wrapper wrapper)
        {

            if (!ValidateField(wrapper, SecurityListFields.SecurityRequestResult))
                throw new Exception("Missing Security Request Result");

            if (!ValidateField(wrapper, SecurityListFields.TotNoRelatedSym))
                throw new Exception("Missing Tot No RelatedSym ");

            if (!ValidateField(wrapper, SecurityListFields.LastFragment))
                throw new Exception("Missing Last Fragment ");
        }

        #endregion

        #region Public Methods

        public SecurityList GetSecurityList(Wrapper wrapper, IConfiguration Config)
        {
            SecurityList sl = new SecurityList();

            ValidateSecurityList(wrapper);

            sl.SecurityRequestResult = (SecurityRequestResult)wrapper.GetField(SecurityListFields.SecurityRequestResult);
            sl.SecurityListRequestType  = (SecurityListRequestType?)(ValidateField(wrapper, SecurityListFields.SecurityListRequestType) ? wrapper.GetField(SecurityListFields.SecurityListRequestType) : null);
            sl.MarketID = (string)(ValidateField(wrapper, SecurityListFields.MarketID) ? wrapper.GetField(SecurityListFields.MarketID) : null);
            sl.MarketSegmentID = (string)(ValidateField(wrapper, SecurityListFields.MarketSegmentID) ? wrapper.GetField(SecurityListFields.MarketSegmentID) : null);
            sl.TotNoRelatedSym = (int)(ValidateField(wrapper, SecurityListFields.TotNoRelatedSym) ? wrapper.GetField(SecurityListFields.TotNoRelatedSym) : null);
            sl.LastFragment = (string)(ValidateField(wrapper, SecurityListFields.LastFragment) ? wrapper.GetField(SecurityListFields.LastFragment) : null);
            sl.TotNoRelatedSym = (int)wrapper.GetField(SecurityListFields.TotNoRelatedSym);
            sl.Securities = new List<Main.BusinessEntities.Securities.Security>();

            SecurityConverter securityConverter = new SecurityConverter();
            List<Wrapper> securityWrappers = (List<Wrapper>)wrapper.GetField(SecurityListFields.Securities);

            if (securityWrappers != null)
            {
                foreach(Wrapper securityWrapper in securityWrappers)
                {
                    try
                    {
                        Security security = securityConverter.GetSecurity(securityWrapper, Config);
                        sl.Securities.Add(security);
                    }
                    catch (Exception ex)
                    { 
                        //TODO: DoLOG
                    }
                }
            }
            
            return sl;
        }


        #endregion
    }
}
