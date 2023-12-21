using System;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace tph.StrategyHandler.SimpleCommandSender.Common.Util
{
    public class HistoricalPricesRequestConverter: ConverterBase
    {
        #region Public Static Methods

        public static HistoricalPricesReqDTO ConvertHistoricalPricesRequest(Wrapper wrapper)
        {

            HistoricalPricesReqDTO dto = new HistoricalPricesReqDTO();

            if (ValidateField(wrapper, HistoricalPricesRequestFields.Symbol))
                dto.Symbol = (string) wrapper.GetField(HistoricalPricesRequestFields.Symbol);
            else
                throw new Exception($"Missing field on Historical Price Request: symbol");
            
            if (ValidateField(wrapper, HistoricalPricesRequestFields.MDReqId))
                dto.HistPrReqId = (int) wrapper.GetField(HistoricalPricesRequestFields.MDReqId);
            else
                throw new Exception($"Missing field on Historical Price Request: MDReqId");

            if (ValidateField(wrapper, HistoricalPricesRequestFields.Exchange))
                dto.Exchange = (string)wrapper.GetField(HistoricalPricesRequestFields.Exchange);
            else
                throw new Exception($"Missing field on Historical Price Request: Exchange");

            if (ValidateField(wrapper, HistoricalPricesRequestFields.Currency))
                dto.Currency = (string)wrapper.GetField(HistoricalPricesRequestFields.Currency);
            else
                throw new Exception($"Missing field on Historical Price Request: Currency");

            if (ValidateField(wrapper, HistoricalPricesRequestFields.SecurityType))
                dto.SecurityType = (SecurityType?)wrapper.GetField(HistoricalPricesRequestFields.SecurityType);
            else
                throw new Exception($"Missing field on Historical Price Request: Exchange");

            if (ValidateField(wrapper, HistoricalPricesRequestFields.From))
                dto.From = (DateTime?) wrapper.GetField(HistoricalPricesRequestFields.From);
            else
                throw new Exception($"Missing field on Historical Price Request: From");
            
            if (ValidateField(wrapper, HistoricalPricesRequestFields.To))
                dto.To = (DateTime?) wrapper.GetField(HistoricalPricesRequestFields.To);
            else
                throw new Exception($"Missing field on Historical Price Request: To");

            if (ValidateField(wrapper, HistoricalPricesRequestFields.Interval))
            {
                CandleInterval cand = (CandleInterval) wrapper.GetField(HistoricalPricesRequestFields.Interval);
                dto.Interval = HistoricalPricesReqDTO.GetStrInterval(cand);
            }
            else
                throw new Exception($"Missing field on Historical Price Request: Interval");

            return dto;

        }

        #endregion
    }
}