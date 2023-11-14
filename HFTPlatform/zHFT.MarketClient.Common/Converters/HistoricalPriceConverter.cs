using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.DTO;

namespace zHFT.MarketClient.Common.Converters
{
    public class HistoricalPriceConverter : ConverterBase
    {
        #region Protected Static Consts

        public static string _DEF_CURRENCY = "USD";

        #endregion

        #region Public Static Methods

        public static HistoricalPricesRequestDTO ConvertHistoricalPriceRequest(Wrapper wrapper)
        {
            HistoricalPricesRequestDTO dto = new HistoricalPricesRequestDTO();

            if (ValidateField(wrapper, HistoricalPricesRequestFields.MDReqId))
                dto.ReqId = (int)wrapper.GetField(HistoricalPricesRequestFields.MDReqId);
            else
                throw new Exception($"Missing mandatory field MDReqId for historical data request");

            if (ValidateField(wrapper, HistoricalPricesRequestFields.Symbol))
                dto.Symbol = (string)wrapper.GetField(HistoricalPricesRequestFields.Symbol);
            else
                throw new Exception($"Missing mandatory field symbol for historical data request");


            if (ValidateField(wrapper, HistoricalPricesRequestFields.Interval))
                dto.Interval =(CandleInterval)wrapper.GetField(HistoricalPricesRequestFields.Interval);
            else
                dto.Interval = CandleInterval.Minute_1;

            if (ValidateField(wrapper, HistoricalPricesRequestFields.Currency))
                dto.Currency = (string)wrapper.GetField(HistoricalPricesRequestFields.Currency);
            else
                dto.Currency = _DEF_CURRENCY;


            if (ValidateField(wrapper, HistoricalPricesRequestFields.SecurityType))
                dto.SecurityType = (SecurityType)wrapper.GetField(HistoricalPricesRequestFields.SecurityType);
            else
                dto.SecurityType = SecurityType.OTH;


            DateTime? from = null;
            DateTime? to = null;
            if (ValidateField(wrapper, HistoricalPricesRequestFields.From))
                dto.From = (DateTime?)wrapper.GetField(HistoricalPricesRequestFields.From);

            if (ValidateField(wrapper, HistoricalPricesRequestFields.To))
                dto.To = (DateTime?)wrapper.GetField(HistoricalPricesRequestFields.To);

            return dto;

        }

        #endregion
    }
}
