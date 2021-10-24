using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstrFullMarketConnectivity.IOL.Common;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.InvertirOnline.Common.DTO;

namespace zHFT.OrderRouters.InvertirOnline.Common.Converters
{
    public class OrderConverter : ConverterBase
    {
        #region Private Static Consts

        private static string _SETTL_T_PLUS_0 = "t0";
        private static string _SETTL_T_PLUS_1 = "t1";
        private static string _SETTL_T_PLUS_2 = "t2";
        private static string _SETTL_T_PLUS_3 = "t3";

        //private static string _SETTL_T_PLUS_0 = "a0horas";
        //private static string _SETTL_T_PLUS_1 = "a24horas";
        //private static string _SETTL_T_PLUS_2 = "a48horas";
        //private static string _SETTL_T_PLUS_3 = "a72horas";

        private static string _ORD_TYPE_LIMIT = "PrecioLimite";
        private static string _ORD_TYPE_MARKET = "PrecioMercado";


        #endregion

        #region Private Static Methods



        private  string ConvertSettlType(SettlType? settlType)
        {
            if (settlType.HasValue)
            {
                if (settlType == SettlType.Regular)
                    return _SETTL_T_PLUS_0;
                else if (settlType == SettlType.NextDay)
                    return _SETTL_T_PLUS_1;
                if (settlType == SettlType.Tplus2)
                    return _SETTL_T_PLUS_2;
                if (settlType == SettlType.Tplus3)
                    return _SETTL_T_PLUS_3;
                else
                    throw new Exception(string.Format("Settlement Type no soportado en Invertir Online:{0}", settlType.Value));

            }
            else
                return _SETTL_T_PLUS_2;
        }

        private  DateTime ConvertTimeInForce(TimeInForce tif)
        {
            if (tif == TimeInForce.Day || tif == TimeInForce.GoodTillCancel)
                return DateTime.Now.AddDays(1);
            else throw new Exception(string.Format("Time In Force no soportado en Invertir Online:{0}", tif));
        }

        #endregion

        #region Public Static Methods

        public  Order GetNewOrder(Wrapper wrapper, int nextOrderId)
        {
            Order order = new Order();

            ValidateNewOrder(wrapper);

            string fullSymbol = (string)wrapper.GetField(OrderFields.Symbol);

            order.simbolo = SymbolConverter.GetCleanSymbolFromFullSymbol(fullSymbol);
            string instrExchange = ExchangeConverter.GetMarketFromFullSymbol(fullSymbol);
            order.mercado = ExchangeConverter.GetIOLMarketFromInstrMarket(instrExchange);

            order.cantidad = Convert.ToInt64(wrapper.GetField(OrderFields.OrderQty)); 
            order.precio = (double?)wrapper.GetField(OrderFields.Price);
            order.side = (Side)wrapper.GetField(OrderFields.Side);
            order.ordtype = (OrdType)wrapper.GetField(OrderFields.OrdType);
            order.modalidad = _ORD_TYPE_MARKET;
            order.OrderId = nextOrderId;
            order.ClOrdId = wrapper.GetField(OrderFields.ClOrdID).ToString();

            TimeInForce tif = (TimeInForce)wrapper.GetField(OrderFields.TimeInForce);
            order.validez = ConvertTimeInForce(tif);

            SettlType? settlType = (zHFT.Main.Common.Enums.SettlType?)wrapper.GetField(OrderFields.SettlType);
            order.plazo = ConvertSettlType(settlType);

            return order;
        }


        #endregion
    }
}
