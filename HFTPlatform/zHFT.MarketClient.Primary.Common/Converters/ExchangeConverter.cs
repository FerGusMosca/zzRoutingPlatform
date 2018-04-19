using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.MarketClient.Primary.Common.Converters
{
    public class ExchangeConverter
    {
        #region Private Static Conts

        private static string _BYMA = "BUE";
        private static string _ROFX = "ROFX";
        private static string _BYMA_PRIMARY_PREFIX_CODE = "MERV - XMEV";
        private static string _ROFX_PRIMARY_PREFIX_CODE = "";

        private static string _BYMA_CS_CLEARINGID = "48hs";
        private static string _BYMA_OPT_CLEARINGID = "24hs";
        private static string _BYMA_BOND_CLEARINGID = "48hs";
        private static string _BYMA_BILL_CLEARINGID = "CI";
        private static string _ROFX_CLEARINGID = "";

        #endregion

        #region Public Static Methods

        public static string GetMarketPrefixCode(string exchange)
        {
            if (exchange == _BYMA)
            {
                return _BYMA_PRIMARY_PREFIX_CODE;
            }
            else if (exchange == _ROFX)
            {
                return _ROFX_PRIMARY_PREFIX_CODE;
            }

            else
                throw new Exception(string.Format("Exchange translation not implemented {0}", exchange));
        
        }

        public static bool IsValidClearingId(string primarySymbol, SecurityType secType)
        {
            string exchange = GetMarketFromPrimarySymbol(primarySymbol);

            if (exchange == _ROFX)
            {
                return true;

            }
            else
            {
                if (secType == SecurityType.CS)
                {
                    return primarySymbol.EndsWith(_BYMA_CS_CLEARINGID);

                }
                else if (secType == SecurityType.OPT)
                {
                    return primarySymbol.EndsWith(_BYMA_OPT_CLEARINGID);
                }
                else if (secType == SecurityType.TBOND)
                {
                    return primarySymbol.EndsWith(_BYMA_BOND_CLEARINGID);
                }
                else if (secType == SecurityType.TB)
                {
                    return primarySymbol.EndsWith(_BYMA_BILL_CLEARINGID);
                }
                else
                    return false;
            }

        }

        public static string GetMarketClearingID(SecurityType secType,string exchange)
        {
            if (exchange == _ROFX)
            {
                return _ROFX_CLEARINGID;

            }
            else
            {
                if (secType == SecurityType.CS)
                {
                    if (exchange == _BYMA)
                        return _BYMA_CS_CLEARINGID;

                }
                else if (secType == SecurityType.OPT)
                {
                    if (exchange == _BYMA)
                        return _BYMA_OPT_CLEARINGID;
                }
                else if (secType == SecurityType.TBOND)
                {
                    if (exchange == _BYMA)
                        return _BYMA_BOND_CLEARINGID;
                }
                else if (secType == SecurityType.TB)
                {
                    if (exchange == _BYMA)
                        return _BYMA_BILL_CLEARINGID;
                }
            }

            throw new Exception(string.Format("There is no clearing ID code set for market {0} and sec type {1}", exchange, secType.ToString()));
        
        }

        public static SettlType GetMarketSettlTypeID(SecurityType secType, string exchange)
        {
            if (exchange == _ROFX)
            {
                return SettlType.Future;

            }
            else
            {
                if (secType == SecurityType.CS)
                {
                    if (exchange == _BYMA)
                        return SettlType.Tplus2;

                }
                else if (secType == SecurityType.OPT)
                {
                    if (exchange == _BYMA)
                        return SettlType.NextDay;
                }
                else if (secType == SecurityType.TBOND)
                {
                    if (exchange == _BYMA)
                        return SettlType.Tplus2;
                }
                else if (secType == SecurityType.TB)
                {
                    if (exchange == _BYMA)
                        return SettlType.Tplus2;
                }
            }

            throw new Exception(string.Format("There is no clearing ID code set for market {0} and sec type {1}", exchange, secType.ToString()));

        }

        public static string GetMarketFromPrimarySymbol(string primarySymbol)
        {
            if (primarySymbol.StartsWith(_BYMA_PRIMARY_PREFIX_CODE))
            {
                return _BYMA;
            }
            else
                return _ROFX;

        }

        public static string GetMarketFromFullSymbol(string fullSymbol)
        {
            if (fullSymbol.EndsWith(_BYMA))
            {
                return _BYMA;
            }
            else if (fullSymbol.EndsWith(_ROFX))
                return _ROFX;
            else
                 throw new Exception(string.Format("No se pudo procesar el mercado para el symbol {0}",fullSymbol));

        }

        #endregion

    }
}
