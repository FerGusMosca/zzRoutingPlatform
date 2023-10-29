using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.Common.Util
{
    public class SecurityTypeTranslator
    {

        #region Public Static Methods

        public static SecurityType? TranslateNonMandatorySecurityType(string securityType)
        {

            if (securityType == SecurityType.CS.ToString())
                return SecurityType.CS;
            else if (securityType == SecurityType.FUT.ToString())
                return SecurityType.FUT;
            else if (securityType == SecurityType.OPT.ToString())
                return SecurityType.OPT;
            else if (securityType == SecurityType.IND.ToString())
                return SecurityType.IND;
            else if (securityType == SecurityType.CASH.ToString())
                return SecurityType.CASH;
            else if (securityType == SecurityType.TBOND.ToString())
                return SecurityType.TBOND;
            else if (securityType == SecurityType.TB.ToString())
                return SecurityType.TB;
            else if (securityType == SecurityType.IRS.ToString())
                return SecurityType.IRS;
            else if (securityType == SecurityType.REPO.ToString())
                return SecurityType.REPO;
            else if (securityType == SecurityType.CC.ToString())
                return SecurityType.CC;
            else if (securityType == SecurityType.OTH.ToString())
                return SecurityType.OTH;
            else if (securityType == SecurityType.CMDTY.ToString())
                return SecurityType.CMDTY;
            else if (securityType == SecurityType.SWAP.ToString())
                return SecurityType.SWAP;
            else if (securityType == SecurityType.MF.ToString())
                return SecurityType.MF;
            else
                return null;

        }

        public static SecurityType TranslateMandatorySecurityType(string securityType)
        { 
            SecurityType? secType= TranslateNonMandatorySecurityType(securityType);

            if (!secType.HasValue)
                throw new Exception($"Invalid security type {secType}");

            return secType.Value;
        }



        #endregion


    }
}
