using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.StrategyHandler.Common.Converters
{
    public class SecurityConverter : ConverterBase
    {
        #region Private Methods

        protected void ValidateSecurity(Wrapper wrapper)
        {

        }

        #endregion

        #region Public Methods

        public Security GetSecurity(Wrapper wrapper, IConfiguration Config)
        {
            Security s = new Security();

            ValidateSecurity(wrapper);


            s.Symbol = (string)(ValidateField(wrapper, SecurityFields.Symbol) ? wrapper.GetField(SecurityFields.Symbol) : null);
            s.SecurityDesc = (string)(ValidateField(wrapper, SecurityFields.SecurityDesc) ? wrapper.GetField(SecurityFields.SecurityDesc) : null);
            s.Factor = (double?)(ValidateField(wrapper, SecurityFields.Factor) ? wrapper.GetField(SecurityFields.Factor) : null);
            s.SecType = (SecurityType)(ValidateField(wrapper, SecurityFields.SecurityType) ? wrapper.GetField(SecurityFields.SecurityType) : null);
            s.CFICode = (string)(ValidateField(wrapper, SecurityFields.CFICode) ? wrapper.GetField(SecurityFields.CFICode) : null);
            s.ContractMultiplier = (double?)(ValidateField(wrapper, SecurityFields.ContractMultiplier) ? wrapper.GetField(SecurityFields.ContractMultiplier) : null);
            s.Currency = (string)(ValidateField(wrapper, SecurityFields.Currency) ? wrapper.GetField(SecurityFields.Currency) : null);
            s.Exchange = (string)(ValidateField(wrapper, SecurityFields.Exchange) ? wrapper.GetField(SecurityFields.Exchange) : null);
            s.Halted = (Halted?)(ValidateField(wrapper, SecurityFields.Halted) ? wrapper.GetField(SecurityFields.Halted) : null);
            s.StrikePrice = (double?)(ValidateField(wrapper, SecurityFields.StrikePrice) ? wrapper.GetField(SecurityFields.StrikePrice) : null);
            s.MaturityDate = (DateTime?)(ValidateField(wrapper, SecurityFields.MaturityDate) ? wrapper.GetField(SecurityFields.MaturityDate) : null);
            s.MaturityMonthYear = (string)(ValidateField(wrapper, SecurityFields.MaturityMonthYear) ? wrapper.GetField(SecurityFields.MaturityMonthYear) : null);
            s.SymbolSfx = (string)(ValidateField(wrapper, SecurityFields.SymbolSfx) ? wrapper.GetField(SecurityFields.SymbolSfx) : null);
            s.UnderlyingSymbol = (string)(ValidateField(wrapper, SecurityFields.UnderlyingSymbol) ? wrapper.GetField(SecurityFields.UnderlyingSymbol) : null);
            s.StrikeCurrency = (string)(ValidateField(wrapper, SecurityFields.StrikeCurrency) ? wrapper.GetField(SecurityFields.StrikeCurrency) : null);
            s.MinPriceIncrement = (double?)(ValidateField(wrapper, SecurityFields.MinPriceIncrement) ? wrapper.GetField(SecurityFields.MinPriceIncrement) : null);
            s.TickSize = (double?)(ValidateField(wrapper, SecurityFields.TickSize) ? wrapper.GetField(SecurityFields.TickSize) : null);
            s.InstrumentPricePrecision = (int?)(ValidateField(wrapper, SecurityFields.InstrumentPricePrecision) ? wrapper.GetField(SecurityFields.InstrumentPricePrecision) : null);
            s.InstrumentSizePrecision = (int?)(ValidateField(wrapper, SecurityFields.InstrumentSizePrecision) ? wrapper.GetField(SecurityFields.InstrumentSizePrecision) : null);
            s.ContractPositionNumber = (int?)(ValidateField(wrapper, SecurityFields.ContractPositionNumber) ? wrapper.GetField(SecurityFields.ContractPositionNumber) : null);
            s.MarginRatio = (double?)(ValidateField(wrapper, SecurityFields.MarginRatio) ? wrapper.GetField(SecurityFields.MarginRatio) : null);
            s.ContractSize = (decimal?)(ValidateField(wrapper, SecurityFields.ContractSize) ? wrapper.GetField(SecurityFields.ContractSize) : null);


            return s;
        }

        #endregion
    }
}
