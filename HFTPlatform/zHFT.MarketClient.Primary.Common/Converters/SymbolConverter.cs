using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.MarketClient.Primary.Common.Converters
{
    public class SymbolConverter
    {
        #region Private Static Consts

        private static string _PRIMARY_FIELD_SEPARATOR = "-";
        private static string _INSTR_FIELD_SEPARATOR = ".";
        private static int _SYMBOL_INDEX = 2;
        private static int _EXCHANGE_INDEX = 0;

        private static string _BYMA = "BUE";
        private static string _ROFX = "ROFX";

        #endregion

        #region Public Static Methods
        public static string GetCleanSymbolFromPrimary(string symbol,string exchange)
        {
            if (exchange == _BYMA)
            {
                if (string.IsNullOrEmpty(symbol))
                    throw new Exception(string.Format("Symbol not specified for security"));


                string[] fields = symbol.Split(new string[] { _PRIMARY_FIELD_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

                if (fields.Length <= _SYMBOL_INDEX)
                    throw new Exception(string.Format("Could not find field symbol for security {0}", symbol));

                return fields[_SYMBOL_INDEX].Trim();
            }
            else if (exchange == _ROFX)
            {
                return symbol;
            }
            else
                throw new Exception(string.Format("Exchange symbol translation from Primary not implemented for exchange {0}", exchange));
        }

        public static string GetFullSymbolFromPrimary(string symbol,string market)
        {
            if (market == _BYMA)
            {
                if (string.IsNullOrEmpty(symbol))
                    throw new Exception(string.Format("Symbol not specified for security"));

                if (string.IsNullOrEmpty(market))
                    throw new Exception(string.Format("Market not specified for security"));


                string[] fields = symbol.Split(new string[] { _PRIMARY_FIELD_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

                if (fields.Length <= _SYMBOL_INDEX)
                    throw new Exception(string.Format("Could not find field symbol for security {0}", symbol));

                return fields[_SYMBOL_INDEX].Trim() + _INSTR_FIELD_SEPARATOR + market;
            }
            else if (market == _ROFX)
                return symbol;
            else
                throw new Exception(string.Format("GetFullSymbolFromPrimary translation not implemented for exchange {0}", market));
        }

        public static string GetFullSymbolFromCleanSymbol(string symbol, string market)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new Exception(string.Format("Symbol not specified for security"));

            if (string.IsNullOrEmpty(market))
                return symbol;
            else
            {
                if (!symbol.Contains(string.Format("{0}{1}", _INSTR_FIELD_SEPARATOR, market)))
                    return string.Format("{0}{1}{2}", symbol, _INSTR_FIELD_SEPARATOR, market);
                else
                    return symbol;

            }

        }

        public static string GetCleanSymbolFromFullSymbol(string symbol, string market)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new Exception(string.Format("Symbol not specified for security"));

            if (string.IsNullOrEmpty(market))
                return symbol;

            if (symbol.Contains(string.Format("{0}{1}", _INSTR_FIELD_SEPARATOR, market)))
            {
                string[] fields = symbol.Split(new string[] { _INSTR_FIELD_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

                return fields[0].Trim();
            }
            else
                return symbol;

        }

        public static string GetCleanSymbolFromFullSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new Exception(string.Format("Symbol not specified for security"));

            if (symbol.Contains(string.Format("{0}", _INSTR_FIELD_SEPARATOR)))
            {
                string[] fields = symbol.Split(new string[] { _INSTR_FIELD_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

                return fields[0].Trim();
            }
            else
                return symbol;

        }

        public static string GetSymbolToPrimary(string symbol,string market, string marketPrefixCode,string marketClearingID)
        {

            if (symbol.Contains(_INSTR_FIELD_SEPARATOR + market))
            {
                string[] symbolData = symbol.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);

                if (symbolData[1].Trim() == market)
                    return string.Format("{0} - {1} - {2}", marketPrefixCode, symbolData[0], marketClearingID);
                else
                    throw new Exception(string.Format("@{0}: Could not process security {1} for market {2}", "ProcessSymbolFromInstrToPrimary", symbolData, market));
            }
            else
                return string.Format("{0} - {1} - {2}", marketPrefixCode, symbol, marketClearingID);

        }

        #endregion
    }
}
