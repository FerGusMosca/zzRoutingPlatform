
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.IBR.Binance.Common.Util
{
    //public class BinanceClientProxy : BinanceClient
    //{
    //    #region Constructors

    //    public BinanceClientProxy(ApiClient pApiClient)
    //        : base(pApiClient)
    //    {
    //    }

    //    #endregion

    //    #region Protected Methods

    //    protected IEnumerable<Candlestick> GetParsedCandlestick(dynamic candlestickData)
    //    {
    //        var result = new List<Candlestick>();

    //        foreach (JToken item in ((JArray)candlestickData).ToArray())
    //        {
    //            result.Add(new Candlestick()
    //            {
    //                OpenTime = long.Parse(item[0].ToString(), CultureInfo.InvariantCulture),
    //                Open = decimal.Parse(item[1].ToString(), CultureInfo.InvariantCulture),
    //                High = decimal.Parse(item[2].ToString(), CultureInfo.InvariantCulture),
    //                Low = decimal.Parse(item[3].ToString(), CultureInfo.InvariantCulture),
    //                Close = decimal.Parse(item[4].ToString(), CultureInfo.InvariantCulture),
    //                Volume = decimal.Parse(item[5].ToString(), CultureInfo.InvariantCulture),
    //                CloseTime = long.Parse(item[6].ToString(), CultureInfo.InvariantCulture),
    //                QuoteAssetVolume = decimal.Parse(item[7].ToString(), CultureInfo.InvariantCulture),
    //                NumberOfTrades = int.Parse(item[8].ToString(), CultureInfo.InvariantCulture),
    //                TakerBuyBaseAssetVolume = decimal.Parse(item[9].ToString(), CultureInfo.InvariantCulture),
    //                TakerBuyQuoteAssetVolume = decimal.Parse(item[10].ToString(), CultureInfo.InvariantCulture)
    //            });
    //        }

    //        return result;
    //    }

    //    #endregion

    //    #region Public Methods

    //    public async Task<IEnumerable<Candlestick>> GetLastMinuteCandleStick(string symbol)
    //    {
    //         if (string.IsNullOrWhiteSpace(symbol))
    //        {
    //            throw new ArgumentException("symbol cannot be empty. ", "symbol");
    //        }

    //        //var args = $"symbol={symbol.ToUpper()}&interval={interval.GetDescription()}"
    //        //    + (startTime.HasValue ? $"&startTime={startTime.Value.GetUnixTimeStamp()}" : "")
    //        //    + (endTime.HasValue ? $"&endTime={endTime.Value.GetUnixTimeStamp()}" : "")
    //        //    + $"&limit={limit}";

    //         //string args = string.Format("symbol={0}&interval={1}&startTime={2}&endTime={3}&limit={4}",
    //         //                            symbol, "1m", "", "", 10);

    //         string args = string.Format("symbol={0}&interval={1}&limit={2}",
    //                                      symbol, "1m", 5);

    //         var result = await _apiClient.CallAsync<dynamic>(ApiMethod.GET, EndPoints.Candlesticks, false, args);
           
    //        var parsedResult = GetParsedCandlestick(result);

    //        return parsedResult;
    //    }

    //    #endregion
    //}
}
