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
    public class BitMexSecurityWrapper : Wrapper
    { 
        #region Protected Consts

        #endregion

        #region Protected Attributes

        protected IConfiguration Config { get; set; }

        protected Security Security { get; set; }

        #endregion

        #region Constructors

        public BitMexSecurityWrapper(Security pSecurity, IConfiguration pConfig) 
        {
            Security = pSecurity;

            Config = pConfig;
        }

        #endregion

        #region Private Methods

        private zHFT.Main.Common.Enums.SecurityType GetSecurityType()
        {

            if (Security.SecurityType == zHFT.InstructionBasedMarketClient.BitMex.BE.SecurityType.FUT)
                return zHFT.Main.Common.Enums.SecurityType.FUT;
            else if (Security.SecurityType == zHFT.InstructionBasedMarketClient.BitMex.BE.SecurityType.INDX)
                return zHFT.Main.Common.Enums.SecurityType.IND;
            if (Security.SecurityType == zHFT.InstructionBasedMarketClient.BitMex.BE.SecurityType.OPT)
                return zHFT.Main.Common.Enums.SecurityType.OPT;
            if (Security.SecurityType == zHFT.InstructionBasedMarketClient.BitMex.BE.SecurityType.OTH)
                return zHFT.Main.Common.Enums.SecurityType.OTH;
            if (Security.SecurityType == zHFT.InstructionBasedMarketClient.BitMex.BE.SecurityType.SWAP)
                return zHFT.Main.Common.Enums.SecurityType.SWAP;
            else
                throw new Exception(string.Format("Unknown security type for symbol {0}:{1}", Security.Symbol, Security.SecurityType.ToString()));
        
        }

        #endregion

        #region Public Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            SecurityFields sField = (SecurityFields)field;

            if (Security == null)
                return SecurityFields.NULL;

            if (sField == SecurityFields.Symbol)
                return Security.SecurityAltID;//Ex: XBTZ18
            else if (sField == SecurityFields.SecurityDesc)
                return Security.SecurityAltID;
            else if (sField == SecurityFields.SecurityType)
                return GetSecurityType();
            else if (sField == SecurityFields.Factor)
                return SecurityFields.NULL;
            else if (sField == SecurityFields.CFICode)
                return SecurityFields.NULL;
            else if (sField == SecurityFields.ContractMultiplier)
                return Security.ContractMultiplier;
            else if (sField == SecurityFields.Currency)
                return Security.QuoteSymbol;
            else if (sField == SecurityFields.Exchange)
                return "BitMex";
            else if (sField == SecurityFields.StrikePrice)
                return Security.StrikePrice;
            else if (sField == SecurityFields.MaturityDate)
                return Security.MaturityDate;
            else if (sField == SecurityFields.MaturityMonthYear)
                return Security.MaturityMonthYear;
            else if (sField == SecurityFields.SymbolSfx)
                return Security.Symbol;//Ex: XBT
            else if (sField == SecurityFields.UnderlyingSymbol)
                return Security.Symbol;
            else if (sField == SecurityFields.StrikeCurrency)
                return Security.QuoteSymbol;
            else if (sField == SecurityFields.MinPriceIncrement)
                return Security.MinPriceIncrement;
            else if (sField == SecurityFields.TickSize)
                return SecurityFields.NULL;
            else if (sField == SecurityFields.InstrumentPricePrecision)
                return SecurityFields.NULL;
            else if (sField == SecurityFields.InstrumentSizePrecision)
                return SecurityFields.NULL;
            else if (sField == SecurityFields.ContractPositionNumber)
                return SecurityFields.NULL;
            else if (sField == SecurityFields.MarginRatio)
                return Security.GetMargin();
            else if (sField == SecurityFields.ContractSize)
                return Security.LotSize;
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
