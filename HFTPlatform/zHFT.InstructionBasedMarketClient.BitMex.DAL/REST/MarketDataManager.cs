using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO;
using zHFT.InstructionBasedMarketClient.BitMex.DAL.API;

namespace zHFT.InstructionBasedMarketClient.BitMex.DAL
{
    public class MarketDataManager : BaseManager
    {
        #region Protected Attributes

        public string URL { get; set; }

        #endregion

        #region Private Static Consts

        private static string _INSTRUMENTS = "/instrument";

        private static string _INSTRUMENTS_GET_ACTIVE = "/instrument/active";

        private static string _ORDER_BOOK_L2 = "/orderBook/L2";

        private static string _TRADES = "/trade";

        private static string _QUOTE = "/quote";

        #endregion

        #region Constructors

        public MarketDataManager(string url)
        {
            URL = url;

        }

        #endregion

        #region Public Attributes

       
        public MarketData GetMarketData(string symbol)
        {
            BitMEXApi api = new BitMEXApi(URL);

            var param = new Dictionary<string, string>();
            param.Add("symbol", symbol);
            string resp = api.Query("GET", _INSTRUMENTS, param, false);

            Instrument[] instrArr = JsonConvert.DeserializeObject<Instrument[]>(resp);

            //BitMex doesn't allow to filter by quote symbol so we have to do that in memory
            Instrument instr = instrArr.FirstOrDefault();

            if (instr == null)
                throw new Exception(string.Format("No market data found for pair {0}", symbol));

            MarketData md = MapMarketData(instr);

            return md;

        }

        public List<OrderBookEntry> GetOrderBook(string symbol)
        {
            BitMEXApi api = new BitMEXApi(URL);
            List<OrderBookEntry> orders = new List<OrderBookEntry>();

            var param = new Dictionary<string, string>();
            param.Add("symbol", symbol);
            string resp = api.Query("GET", _ORDER_BOOK_L2, param, false);

            List<OrderBookEntry> orderBookEntryList = JsonConvert.DeserializeObject<List<OrderBookEntry>>(resp);

            return orderBookEntryList;
        }


        //public Quote GetQuote(string symbol)
        //{
        //    BitMEXApi api = new BitMEXApi(URL);
        //    var param = new Dictionary<string, string>();
        //    param.Add("symbol", symbol);
        //    string resp = api.Query("GET", _QUOTE, param, false);

        //    Quote quote = JsonConvert.DeserializeObject<Quote>(resp);

        //    return quote;
        //}

        public List<Trade> GetTrades(string symbol, int count)
        {

            BitMEXApi api = new BitMEXApi(URL);
            List<Trade> trades = new List<Trade>();

            var param = new Dictionary<string, string>();
            param.Add("symbol", symbol);
            param.Add("count", count.ToString());
            param.Add("reverse", true.ToString());
            string resp = api.Query("GET", _TRADES, param, false);

            List<Trade> tradeList = JsonConvert.DeserializeObject<List<Trade>>(resp);

            return tradeList;
        }

        #endregion
    }
}
