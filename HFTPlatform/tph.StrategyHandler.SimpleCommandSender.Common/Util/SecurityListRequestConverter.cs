using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace tph.StrategyHandler.SimpleCommandSender.Common.Util
{
    public class SecurityListRequestConverter : ConverterBase
    {
        #region Public Static Methods

        public static SecurityListReqDTO ConvertSecurityListRequest(Wrapper wrapper)
        {

            SecurityListReqDTO dto = new SecurityListReqDTO();

            if (ValidateField(wrapper, SecurityListRequestField.Symbol))
                dto.Symbol = (string)wrapper.GetField(SecurityListRequestField.Symbol);
            else
                dto.Symbol = null;

            if (ValidateField(wrapper, SecurityListRequestField.SecurityListRequestType))
                dto.SecurityListRequestType = (SecurityListRequestType)wrapper.GetField(SecurityListRequestField.SecurityListRequestType);
            else
                throw new Exception($"Missing field on Security List Request Type: SecurityListRequestType");

            if (ValidateField(wrapper, SecurityListRequestField.SecurityType))
                dto.SecurityType = (SecurityType)wrapper.GetField(SecurityListRequestField.SecurityType);
            else
                throw new Exception($"Missing field on Security List Request: SecurityType");

            if (ValidateField(wrapper, SecurityListRequestField.Currency))
                dto.Currency = (string)wrapper.GetField(SecurityListRequestField.Currency);


            if (ValidateField(wrapper, SecurityListRequestField.Exchange))
                dto.Exchange = (string)wrapper.GetField(SecurityListRequestField.Exchange);
            else
                dto.Exchange = null;

            return dto;

        }

        #endregion
    }
}
