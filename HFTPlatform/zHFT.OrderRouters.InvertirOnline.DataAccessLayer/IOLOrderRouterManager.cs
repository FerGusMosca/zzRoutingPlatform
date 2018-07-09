using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstrFullMarketConnectivity.IOL.DataAccessLayer;
using zHFT.Main.Common.Interfaces;
using zHFT.OrderRouters.InvertirOnline.Common.DTO;
using zHFT.OrderRouters.InvertirOnline.Common.Responses;

namespace zHFT.OrderRouters.InvertirOnline.DataAccessLayer
{
    public class IOLOrderRouterManager : BaseManager
    {
        #region Constructors

        public IOLOrderRouterManager(OnLogMessage OnLogMsg, int pAccountNumber, 
                                    string pCredentialsConnectionString,string pMainURL)
        {
            Name = "Invertir Online Order Router";
            Logger = OnLogMsg;
            CredentialsConnectionString = pCredentialsConnectionString;
            AccountNumber = pAccountNumber;
            MainURL = pMainURL;
            
            LoadConfig();

            Logger("Authenticating Order Routing On Invertir Online", Main.Common.Util.Constants.MessageType.Information);
            Authenticate();
            Logger(string.Format("Order Routing authenticated On Invertir Online. Token:{0}",AuthenticationToken.access_token), 
                   Main.Common.Util.Constants.MessageType.Information);

        
        }

        #endregion

        #region Private Static Consts

        private static string _MARKET_FIELD = "mercado";
        private static string _SYMBOL_FIELD = "simbolo";
        private static string _QTY_FIELD = "cantidad";
        private static string _AMMOUNT_FIELD = "monto";
        private static string _TIF_FIELD = "validez";
        private static string _SETTL_TYPE_FIELD = "plazo";
        private static string _ORD_TYPE_FIELD = "modalidad";
        private static string _PRICE_FIELD = "preciolimite";

        #endregion

        #region Public Methods

        public NewOrderResponse Buy(Order order)
        {

            string url = MainURL + _BUY_URL;
            try
            {
                BaseOrder bOrder = BaseOrder.Clone(order);

                Dictionary<string, string> parameters = new Dictionary<string, string>();

                parameters.Add(_MARKET_FIELD, order.mercado);
                parameters.Add(_SYMBOL_FIELD, order.simbolo);
                parameters.Add(_AMMOUNT_FIELD, (order.cantidad * order.precio).ToString("00.##"));
                //parameters.Add(_QTY_FIELD, order.cantidad.ToString());
                parameters.Add(_TIF_FIELD, order.validez.ToString());
                parameters.Add(_SETTL_TYPE_FIELD, order.plazo);
                parameters.Add(_ORD_TYPE_FIELD, order.modalidad);
                //parameters.Add(_PRICE_FIELD, order.precio.ToString("00.##")); Hasta que no haya execution reports, usamos ordenes market

                string resp = DoPostJson(url, parameters);

                NewOrderResponse noResp = JsonConvert.DeserializeObject<NewOrderResponse>(resp);

                return noResp;

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Se produjo un error enviando una orden de compra el activo {0}:{1}", order.simbolo, ex.Message));
            }
        }

        public NewOrderResponse Sell(Order order)
        {

            string url = MainURL + _SELL_URL;
            try
            {
                BaseOrder bOrder = BaseOrder.Clone(order);

                Dictionary<string, string> parameters = new Dictionary<string, string>();

                parameters.Add(_MARKET_FIELD, order.mercado);
                parameters.Add(_SYMBOL_FIELD, order.simbolo);
                parameters.Add(_QTY_FIELD, order.cantidad.ToString());
                parameters.Add(_TIF_FIELD, order.validez.ToString());
                parameters.Add(_SETTL_TYPE_FIELD, order.plazo);
                parameters.Add(_ORD_TYPE_FIELD, order.modalidad);
                parameters.Add(_PRICE_FIELD, order.precio.ToString("00.##"));

                string resp = DoPostJson(url, parameters);

                NewOrderResponse noResp = JsonConvert.DeserializeObject<NewOrderResponse>(resp);

                return noResp;

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Se produjo un error enviando una orden de venta del activo {0}:{1}", order.simbolo, ex.Message));
            }
        }

        #endregion
    }
}
