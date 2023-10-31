using IBApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;

namespace zHFT.OrderRouters.IB.Common.Converters
{
    public class OrderConverter : ConverterBase
    {
        #region Private Static Consts

        private static string _SECURITY_TYPE_COMMON_STOCK = "STK";
        private static string _SECURITY_TYPE_FUTURE = "FUT";
        private static string _SECURITY_TYPE_OPTIONS = "OPT";
        private static string _SECURITY_TYPE_INDEX = "IND";
        private static string _SECURITY_TYPE_CASH = "CASH";

        private static string _ORDER_TYPE_LIMIT = "LMT";
        private static string _ORDER_TYPE_MARKET = "MKT";

        private static string _SIDE_BUY = "BUY";
        private static string _SIDE_SELL = "SELL";

        private static string _TIF_DAY = "DAY";
        private static string _TIF_GTC = "GTC";
        private static string _TIF_DTC = "DTC";
        private static string _TIF_FOK = "FOK";

        #endregion

        #region Public Methods

        #region Order

        public void AssignOrdType(Order orderInfo, OrdType ordType,double? price)
        {
            if (ordType == OrdType.Limit)
            {
                // Submit a Limit Order (can be LMT, MKT or STP)
                orderInfo.OrderType = _ORDER_TYPE_LIMIT;
                // The limit price of the order
                if (price.HasValue)
                    orderInfo.LmtPrice = price.Value;
                else
                    throw new Exception("Missing price for a limit order");
            }
            else if (ordType == OrdType.Market)
            {
                orderInfo.OrderType = _ORDER_TYPE_MARKET;
            }
            else
                throw new Exception(string.Format("Order type {0} not supported yet", ordType.ToString()));
        
        }

        public void AssignSide(Order orderInfo, Side side)
        {
            if (side == Side.Buy)
            {
                // The Action will be to buy (can be BUY, SELL, SSHORT)
                orderInfo.Action = _SIDE_BUY;
            }
            else if (side == Side.Sell)
                orderInfo.Action = _SIDE_SELL;
            else
                throw new Exception(string.Format("Side {0} not supported yet", side.ToString()));
        
        }

        public void AssignTimeInForce(Order orderInfo, TimeInForce tif)
        {
            // Time In Force (Tif) can be DAY, GTC, IOC, GTD etc.
            // There are about 40 other properties for different order types...
            // We do not need to specify them in this case

            if (tif == TimeInForce.Day)
            {
                orderInfo.Tif = _TIF_DAY;
            }
            else if (tif == TimeInForce.GoodTillCancel)
            {
                orderInfo.Tif = _TIF_GTC;
            }
            else
                throw new Exception(string.Format("Time In Force {0} not supported yet", tif.ToString()));

        }

        #endregion

        #region Contract

        public void AssginContractType(Contract contract, SecurityType type)
        {
            if (type == SecurityType.CS)
                contract.SecType = _SECURITY_TYPE_COMMON_STOCK;
            else if (type == SecurityType.FUT)
                contract.SecType = _SECURITY_TYPE_FUTURE;
            else if (type == SecurityType.OPT)
                contract.SecType = _SECURITY_TYPE_OPTIONS;
            else if (type == SecurityType.IND)
                contract.SecType = _SECURITY_TYPE_INDEX;
            else if (type == SecurityType.CASH)
                contract.SecType = _SECURITY_TYPE_CASH;
            else
                throw new Exception(string.Format("Could not process security type {0}", type.ToString()));
        
        }

        #endregion


        #endregion
    }
}
