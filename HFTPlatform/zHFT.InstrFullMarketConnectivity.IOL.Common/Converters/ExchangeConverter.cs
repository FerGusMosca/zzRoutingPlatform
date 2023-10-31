using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstrFullMarketConnectivity.IOL.Common
{
    public class ExchangeConverter
    {
        #region Private Consts

        private static string _BYMA = "BUE";
        private static string _ROFX = "ROFX";

        private static string _IOL_BYMA = "BCBA";
        private static string _IOL_ROFX = "ROFX";

        private static string _IOL_BYMA_INT = "1";

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
            else if (!fullSymbol.Contains("."))
                return fullSymbol;
            else
                throw new Exception(string.Format("No se pudo procesar el mercado para el symbol {0}", fullSymbol));

        }

        public static string GetIOLMarketFromInstrMarket(string exchange)
        {
            if (exchange.Equals(_BYMA))
            {
                return _IOL_BYMA;
            }
            else if (exchange.Equals(_ROFX))
                return _IOL_ROFX;
            else
                throw new Exception(string.Format("No se pudo procesar el mercado {0}", exchange));

        
        }

        public static string GetInstrMarketFromIolMarket(string exchange)
        {
            if (exchange.ToUpper().Equals(_IOL_BYMA.ToUpper()))
            {
                return _BYMA;
            }
            else if (exchange.ToUpper().Equals(_IOL_BYMA_INT.ToUpper()))
            {
                return _BYMA;
            }
            else if (exchange.ToUpper().Equals(_IOL_ROFX.ToUpper()))
                return _ROFX;
            else
                throw new Exception(string.Format("No se pudo procesar el mercado {0}", exchange));


        }

        #endregion
    }
}
