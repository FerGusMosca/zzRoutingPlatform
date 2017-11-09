using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedFullMarketConnectivity.Primary.Common.Converters
{
    public class SecurityConverter
    {
        #region Private Static Consts

        private static string _PRIMARY_FIELD_SEPARATOR = "-";
        private static string _INSTR_FIELD_SEPARATOR = ".";
        private static int _SYMBOL_INDEX = 2;
        private static int _EXCHANGE_INDEX = 0;

        #endregion

        #region Public Static Methods
        public static string GetCleanSymbolFromPrimary(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new Exception(string.Format("No se especificó el Symbol para el security y el mismo es un campo obligatorio"));


            string[] fields = symbol.Split(new string[] { _PRIMARY_FIELD_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length <= _SYMBOL_INDEX)
                throw new Exception(string.Format("No se puede encontrar el nombre del campo Symbol para el Symbol {0}", symbol));

            return fields[_SYMBOL_INDEX].Trim();

        }

        public static string GetFullSymbolFromCleanSymbol(string symbol, string market)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new Exception(string.Format("No se especificó el Symbol para el security"));

            if (string.IsNullOrEmpty(market))
                return symbol;
            else
            {
                return string.Format("{0}{1}{2}", symbol, _INSTR_FIELD_SEPARATOR, market);
            
            }
        
        }

        public static string GetCleanSymbolFromInstr(string symbol, string market)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new Exception(string.Format("No se especificó el Symbol para el security"));

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

        public static string ProcessSymbolFromInstrToPrimary(string symbol, Configuration.Configuration config)
        {

            if (symbol.Contains(_INSTR_FIELD_SEPARATOR + config.Market))
            {
                string[] symbolData = symbol.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);

                if (symbolData[1].Trim() == config.Market)
                    return string.Format("{0} - {1} - {2}", config.MarketPrefixCode, symbolData[0], config.MarketClearingID);
                else
                    throw new Exception(string.Format("@{0}: Could not process security {1} for market {2}", config.Name, symbolData, config.Market));
            }
            else
                return string.Format("{0} - {1} - {2}", config.MarketPrefixCode, symbol, config.MarketClearingID);

        }

        #endregion
    }
}
