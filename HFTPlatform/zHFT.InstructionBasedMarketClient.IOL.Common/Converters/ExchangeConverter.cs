using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.IOL.Common.Converters
{
    public class ExchangeConverter
    {
        #region Private Consts

        private static string _BYMA = "BUE";
        private static string _ROFX = "ROFX";

        #endregion

        #region Public Static Methods

        public static string GetMarketFromFullSymbol(string fullSymbol)
        {
            if (fullSymbol.EndsWith(_BYMA))
            {
                return _BYMA;
            }
            else if (fullSymbol.EndsWith(_ROFX))
                return _ROFX;
            else
                throw new Exception(string.Format("No se pudo procesar el mercado para el symbol {0}", fullSymbol));

        }

        #endregion
    }
}
