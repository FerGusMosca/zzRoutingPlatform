using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Wrappers;

namespace zHFT.StrategyHandler.Common.Wrappers
{
    public class SecurityWrapper:Wrapper
    {
        #region Protected Attributes

        public Security Security { get; set; }

        protected IConfiguration Config { get; set; }


        #endregion

        #region Constructors

        public SecurityWrapper(Security pSecurity, IConfiguration pConfig) 
        {
            Security = pSecurity;

            Config = pConfig;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            if (Security != null)
            {
                return "";//TO DO : Desarrollar el método to string
            }
            else
                return "";
        }


        public override object GetField(Main.Common.Enums.Fields field)
        {
            SecurityFields sField = (SecurityFields)field;

            if (Security == null)
                return SecurityFields.NULL;

            if (sField == SecurityFields.Symbol)
                return Security.Symbol;
            else if (sField == SecurityFields.SecurityType)
                return Security.SecType;
            else if (sField == SecurityFields.Currency)
                return Security.Currency;
            else if (sField == SecurityFields.Exchange)
                return Security.Exchange;
            else if (sField == SecurityFields.Halted)
                return Security.Halted;
            else if (sField == SecurityFields.SecurityDesc)
                return Security.SecurityDesc;
            else if (sField == SecurityFields.StrikePrice)
                return Security.StrikePrice;
            else if (sField == SecurityFields.MaturityDate)
                return Security.MaturityDate;
            else if (sField == SecurityFields.MaturityMonthYear)
                return Security.MaturityMonthYear;
            else if (sField == SecurityFields.SymbolSfx)
                return Security.SymbolSfx;
            else if (sField == SecurityFields.StrikeCurrency)
                return Security.StrikeCurrency;
            else if (sField == SecurityFields.UnderlyingSymbol)
                return Security.UnderlyingSymbol;
            else if (sField == SecurityFields.PutOrCall)
                return Security.PutOrCall;
            else if (sField == SecurityFields.StrikeMultiplier)
                return Security.StrikeMultiplier;
            else if (sField == SecurityFields.AltIntSymbol)
                return Security.AltIntSymbol;
            else if (sField == SecurityFields.MarketData)
                return new MarketDataWrapper(Security, Config);


            return ExecutionReportFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.SECURITY;
        }

        #endregion
    }
}
