using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Common.Wrappers;
using zHFT.StrategyHandler.Common.DTO;

namespace zHFT.StrategyHandler.Common.Converters
{
    public class HistoricalPricesConverter : ConverterBase
    {
        #region Public Static Methods

        public static HistoricalPricesDTO ConvertHistoricalPrices(HistoricalPricesWrapper wrapper)
        {
            Security sec= null;
            if (ValidateField(wrapper, HistoricalPricesFields.Security))
                sec = (Security)wrapper.GetField(HistoricalPricesFields.Security);
            else
                throw new Exception($"Missing mandatory field Security for Historical Prices");


            int reqId;
            if (ValidateField(wrapper, HistoricalPricesFields.RequestId))
                reqId = (int)wrapper.GetField(HistoricalPricesFields.RequestId);
            else
                throw new Exception($"Missing mandatory field RequestId for Historical Prices");


            CandleInterval interval;
            if (ValidateField(wrapper, HistoricalPricesFields.Interval))
                interval = (CandleInterval)wrapper.GetField(HistoricalPricesFields.Interval);
            else
                throw new Exception($"Missing mandatory field Interval for Historical Prices");

            List<Wrapper> marketDataListWrapper;
            if (ValidateField(wrapper, HistoricalPricesFields.Candles))
                marketDataListWrapper = (List<Wrapper>)wrapper.GetField(HistoricalPricesFields.Candles);
            else
                throw new Exception($"Missing mandatory field Candles for Historical Prices");


            List<MarketData> mdList = new List<MarketData>();
            MarketDataConverter conv = new MarketDataConverter();
            foreach (Wrapper mdWrapper in marketDataListWrapper)
            {

                MarketData md =conv.GetMarketData(mdWrapper, null);
                mdList.Add(md);
            }


            HistoricalPricesDTO dto = new HistoricalPricesDTO()
            {
                Symbol=sec.Symbol,
                Interval=interval,
                MarketData= mdList,
                ReqId=reqId
            };

            return dto;
        }

        #endregion


    }
}
