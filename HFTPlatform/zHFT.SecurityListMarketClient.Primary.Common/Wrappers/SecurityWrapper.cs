using Shared.Bussiness.Fix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;

namespace zHFT.SecurityListMarketClient.Primary.Common.Wrappers
{
    public class SecurityWrapper : Wrapper
    {
        #region Protected Consts

        private int _TICK_SIZE_FIELD = 5023;
        private int _INSTRUMENT_PRICE_PRECISION_FIELD = 5514;
        private int _INSTRUMENT_SIZE_PRECISION_FIELD = 7117;
        private int _CONTRACT_POSITION_NUMBER_FIELD = 9996;

        private static string _FUTURES_PREFIX = "F";
        private static string _OPTIONS_PREFIX = "O";
        private static string _STOCK_PREFIX = "ES";
        private static string _BOND_PREFIX = "DB";
        private static string _SWAP_PREFIX = "XXW";
        private static string _CEDEAR_PREFIX = "EM";
        private static string _FINANTIAL_TRUST_PREFIX = "DT";
        private static string _REPO_PREFIX = "RP";
        private static string _MRI_PREFIX = "MRI";

        private static string _FIELD_SEPARATOR="-";
        private static int _SYMBOL_INDEX = 2;
        private static int _EXCHANGE_INDEX = 0;

        #endregion

        #region Protected Attributes

        protected IConfiguration Config { get; set; }

        protected QuickFix50.SecurityList.NoRelatedSym Security { get; set; }

        #endregion

        #region Constructors

        public SecurityWrapper(QuickFix50.SecurityList.NoRelatedSym pSecurity, 
                               IConfiguration pConfig) 
        {
            Security = pSecurity;

            Config = pConfig;
        }

        #endregion

        #region Private Methods

        private string GetCleanSymbol(string symbol)
        {

            if (string.IsNullOrEmpty(symbol))
                throw new Exception(string.Format("No se especificó el Symbol para el security y el mismo es un campo obligatorio"));


            string[] fields = symbol.Split(new string[] { _FIELD_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length <= _SYMBOL_INDEX)
                throw new Exception(string.Format("No se puede encontrar el nombre del campo Symbol para el Symbol {0}", symbol));

            return fields[_SYMBOL_INDEX];
        }

        private string GetCleanMarket(string securityDesc)
        {

            if (string.IsNullOrEmpty(securityDesc))
                throw new Exception(string.Format("No se especificó el SecurityDesc para el security y el mismo es un campo obligatorio"));


            string[] fields = securityDesc.Split(new string[] { _FIELD_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length <= _EXCHANGE_INDEX)
                throw new Exception(string.Format("No se puede encontrar el nombre del campo Exchange para el SecurityDesc {0}", securityDesc));

            return fields[_EXCHANGE_INDEX];
        }

        private zHFT.Main.Common.Enums.SecurityType GetSecurityTypeByCFICode(string CFICode)
        {
            if (CFICode == null)
                return SecurityType.OTH;


            if (CFICode.StartsWith(_FUTURES_PREFIX))
                return SecurityType.FUT;
            else if (CFICode.StartsWith(_OPTIONS_PREFIX))
                return SecurityType.OPT;
            else if (CFICode.StartsWith(_STOCK_PREFIX))
                return SecurityType.CS;
            else if (CFICode.StartsWith(_BOND_PREFIX))
                return SecurityType.TB;
            else if (CFICode.StartsWith(_SWAP_PREFIX))
                return SecurityType.IRS;
            else if (CFICode.StartsWith(_CEDEAR_PREFIX))
                return SecurityType.CS;
            else if (CFICode.StartsWith(_FINANTIAL_TRUST_PREFIX))
                return SecurityType.OTH;
            else if (CFICode.StartsWith(_REPO_PREFIX))
                return SecurityType.REPO;
            else if (CFICode.StartsWith(_MRI_PREFIX))
                return SecurityType.IND;
            else
                return SecurityType.OTH; 
        }

        private zHFT.Main.Common.Enums.SecurityType? GetSecurityType(string secType)
        {
            if (string.IsNullOrEmpty(secType))
                return SecurityType.OTH;

            if (secType.ToUpper() == SecurityType.CASH.ToString())
                return SecurityType.CASH;
            else if (secType.ToUpper() == SecurityType.CS.ToString())
                return SecurityType.CS;
            else if (secType.ToUpper() == SecurityType.FUT.ToString())
                return SecurityType.FUT;
            else if (secType.ToUpper() == SecurityType.IND.ToString())
                return SecurityType.IND;
            else if (secType.ToUpper() == SecurityType.OPT.ToString())
                return SecurityType.OPT;
            else if (secType.ToUpper() == SecurityType.OTH.ToString())
                return SecurityType.OTH;
            else
                return SecurityType.OTH;
        }

        #endregion

        #region Public Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            SecurityFields sField = (SecurityFields)field;

            if (Security == null)
                return SecurityFields.NULL;

            if (sField == SecurityFields.Symbol)
                return GetCleanSymbol(FixHelperExtended.GetNullFieldIfSet(Security, QuickFix.Symbol.FIELD));
            else if (sField == SecurityFields.SecurityDesc)
                return FixHelperExtended.GetNullFieldIfSet(Security, QuickFix.SecurityDesc.FIELD);
            else if (sField == SecurityFields.SecurityType)
                return GetSecurityTypeByCFICode(FixHelperExtended.GetNullFieldIfSet(Security, QuickFix.CFICode.FIELD));
            else if (sField == SecurityFields.Factor)
                return FixHelperExtended.GetNullDoubleFieldIfSet(Security, QuickFix.Factor.FIELD);
            else if (sField == SecurityFields.CFICode)
                return FixHelperExtended.GetNullFieldIfSet(Security, QuickFix.CFICode.FIELD);
            else if (sField == SecurityFields.ContractMultiplier)
                return FixHelperExtended.GetNullDoubleFieldIfSet(Security, QuickFix.ContractMultiplier.FIELD);
            else if (sField == SecurityFields.Currency)
                return FixHelperExtended.GetNullFieldIfSet(Security, QuickFix.Currency.FIELD);
            else if (sField == SecurityFields.Exchange)
                return GetCleanMarket(FixHelperExtended.GetNullFieldIfSet(Security, QuickFix.SecurityDesc.FIELD));
            else if (sField == SecurityFields.StrikePrice)
                return FixHelperExtended.GetNullDoubleFieldIfSet(Security, QuickFix.StrikePrice.FIELD);
            else if (sField == SecurityFields.MaturityDate)
                return FixHelperExtended.GetNullDateFieldIfSet(Security, QuickFix.MaturityDate.FIELD, false);
            else if (sField == SecurityFields.MaturityMonthYear)
                return FixHelperExtended.GetNullFieldIfSet(Security, QuickFix.MaturityMonthYear.FIELD);
            else if (sField == SecurityFields.SymbolSfx)
                return FixHelperExtended.GetNullFieldIfSet(Security, QuickFix.SymbolSfx.FIELD);
            else if (sField == SecurityFields.StrikeCurrency)
                return FixHelperExtended.GetNullFieldIfSet(Security, QuickFix.StrikeCurrency.FIELD);
            else if (sField == SecurityFields.MinPriceIncrement)
                return FixHelperExtended.GetNullDoubleFieldIfSet(Security, QuickFix.MinPriceIncrement.FIELD);
            else if (sField == SecurityFields.TickSize)
                return FixHelperExtended.GetNullDoubleFieldIfSet(Security, _TICK_SIZE_FIELD);
            else if (sField == SecurityFields.InstrumentPricePrecision)
                return FixHelperExtended.GetNullIntFieldIfSet(Security, _INSTRUMENT_PRICE_PRECISION_FIELD);
            else if (sField == SecurityFields.InstrumentSizePrecision)
                return FixHelperExtended.GetNullIntFieldIfSet(Security, _INSTRUMENT_SIZE_PRECISION_FIELD);
            else if (sField == SecurityFields.ContractPositionNumber)
                return FixHelperExtended.GetNullIntFieldIfSet(Security, _CONTRACT_POSITION_NUMBER_FIELD);
            else if (sField == SecurityFields.MarketData)
                return null;
            
            return SecurityFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.SECURITY;
        }
        #endregion
    }
}
