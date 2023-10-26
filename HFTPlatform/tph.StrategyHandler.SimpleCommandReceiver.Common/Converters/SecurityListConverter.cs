using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.StrategyHandler.Common.Converters;


namespace tph.StrategyHandler.SimpleCommandReceiver.Common.Converters
{
    public class SecurityListConverter
    {

        #region Protected Methods

        protected static bool ValidateField(zHFT.Main.Common.Wrappers.Wrapper wrapper, Fields field)
        {
            return wrapper.GetField(field) != Fields.NULL;
        }

        #endregion

        #region Public Static Methods

        public static SecurityListDTO ConvertSecurityList(SecurityListWrapper securityListWrapper)
        {

            int securityListReqId = 0;
            if (ValidateField(securityListWrapper, SecurityListFields.SecurityListRequestId))
                securityListReqId = (int)securityListWrapper.GetField(SecurityListFields.SecurityListRequestId);
            else
                throw new Exception($"Missing mandatory fieldSecurityListRequestId for Security List");


            SecurityListRequestType type;
            if (ValidateField(securityListWrapper, SecurityListFields.SecurityListRequestType))
                type = (SecurityListRequestType)securityListWrapper.GetField(SecurityListFields.SecurityListRequestType);
            else
                throw new Exception($"Missing mandatory SecurityListRequestType for Security List");

            List<zHFT.Main.Common.Wrappers.Wrapper> securitiesWrapper;
            if (ValidateField(securityListWrapper, SecurityListFields.Securities))
                securitiesWrapper = (List<zHFT.Main.Common.Wrappers.Wrapper>)securityListWrapper.GetField(SecurityListFields.Securities);
            else
                throw new Exception($"Missing mandatory SecurityListRequestType for Security List");


            List<Security> securities = new List<Security>();
            foreach (zHFT.Main.Common.Wrappers.Wrapper secWr in securitiesWrapper)
            {
                SecurityConverter conv = new SecurityConverter();
                Security sec=conv.GetSecurity(secWr, null);
                securities.Add(sec);
            }


            SecurityListDTO dto = new SecurityListDTO()
            {
                Securities = securities,
                SecurityListRequestId = securityListReqId,
                SecurityListRequestType = type

            };

            return dto;

        }

        #endregion
    }
}
