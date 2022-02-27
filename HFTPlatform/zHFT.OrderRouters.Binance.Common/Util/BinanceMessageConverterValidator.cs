using System;
using System.Linq;
using Binance.Net.Objects.Models.Spot;

namespace zHFT.OrderRouters.BINANCE.Common.Util
{
    public class DecimalPrecissionConverter
    {
        #region Public Static Attributes

        public static BinanceExchangeInfo ExchangeInfo { get; set; }

        #endregion

        #region Private Static Methods

        private static int GetDecimalPlaces(decimal number)
        {
            int decimalPl = 0;
            for (int i = 0; i < 10; i++)
            {
                decimalPl = i;
                double divisor = 1 / Math.Pow(10, i);
                double result = Convert.ToDouble(number) % divisor;

                if (result == 0)
                    break;
            }

            return decimalPl;
        }
        
        public static decimal Floor(decimal value, int decimalPlaces)
        {
            decimal adjustment = Convert.ToDecimal(Math.Pow(10, decimalPlaces));
            return Math.Floor(value * adjustment) / adjustment;
        }

        #endregion

        #region Public Static Methods

        public static decimal GetQuantity(string symbol, string quoteSymbol, decimal prevQty)
        {
            BinanceSymbol tradedSymbol = ExchangeInfo.Symbols
                .Where((x => x.BaseAsset.ToUpper() == symbol.ToUpper()
                             && x.QuoteAsset == quoteSymbol))
                .FirstOrDefault();
            
            if(tradedSymbol==null)
                throw new Exception(string.Format("Could not find symbol {0} traded in Binance",symbol));

            if (prevQty < tradedSymbol.LotSizeFilter.MinQuantity)
                throw new Exception(string.Format("Trade Qty. {0} cannot be lower than {1}", prevQty,
                    tradedSymbol.LotSizeFilter.MinQuantity));

            if (prevQty > tradedSymbol.LotSizeFilter.MaxQuantity)
                throw new Exception(string.Format("Trade Qty. {0} cannot be higher than {1}", prevQty,
                    tradedSymbol.LotSizeFilter.MinQuantity));

            int decPlaces = GetDecimalPlaces(tradedSymbol.LotSizeFilter.StepSize);

            return Floor(prevQty, decPlaces);
        }

        public static void ValidateNewOrder(string symbol, string quoteSymbol,decimal qty, double price)
        {
            BinanceSymbol tradedSymbol = ExchangeInfo.Symbols
                                        .Where((x => x.BaseAsset.ToUpper() == symbol.ToUpper()
                                                     && x.QuoteAsset == quoteSymbol))
                                        .FirstOrDefault();
            
            if(tradedSymbol==null)
                throw new Exception(string.Format("Could not find traded symbol {0} at Binance",symbol));

            if (qty * Convert.ToDecimal(price) < tradedSymbol.MinNotionalFilter.MinNotional)
                throw new Exception(string.Format("Order notional must be bigger than {0} {1}"
                    , tradedSymbol.MinNotionalFilter.MinNotional, quoteSymbol));
        }

        #endregion
    }
}