using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.IOL.Common.Converters
{
    public class SymbolConverter
    {
        #region Private Static Consts

        private static string _INSTR_FIELD_SEPARATOR = ".";

        #endregion


        #region Public Static Methods

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


        #endregion
    }
}
