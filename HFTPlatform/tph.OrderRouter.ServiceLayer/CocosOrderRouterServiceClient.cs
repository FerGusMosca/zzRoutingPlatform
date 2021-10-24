using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using tph.OrderRouter.Cocos.Common.DTO.Generic;
using tph.OrderRouter.Cocos.Common.DTO.Orders;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace tph.OrderRouter.ServiceLayer
{
    public class CocosOrderRouterServiceClient:BaseServiceClient
    {
        #region Public Static Consts

        public static string _SIDE_BUY = "1";
        public static string _SIDE_SELL = "2";

        public static string _SETTL_SPOT = "1";
        public static string _SETTL_24HS = "2";
        public static string _SETTL_48HS = "3";
        
        
        #endregion
        
        #region Constructors

        public CocosOrderRouterServiceClient(string pBaseURL,string pDNI, string pUser, 
                                             string pPassword)
        {
            BaseURL = pBaseURL;
            DNI = pDNI;
            User = pUser;
            Password = pPassword;

            DoAuthenticate();
        }

        #endregion
        
        #region Protected Methods


        public string GetTIF(Order order)
        {
            if (order.TimeInForce == TimeInForce.Day)
                return DateTime.Now.ToString("dd/MM/yyyyy");
            else
                throw new Exception(String.Format("TimeInForce not implemented @Cocos:{0}", order.TimeInForce));
        }

        public string GetSettlement(Order order)
        {
            if (order.Security.SecType == SecurityType.CS)
                return _SETTL_48HS;
            else if (order.Security.SecType == SecurityType.OPT)
                return _SETTL_24HS;
            else if (order.Security.SecType == SecurityType.TBOND)
                return _SETTL_48HS;
            else if (order.Security.SecType == SecurityType.TB)
                return _SETTL_24HS;
            else
                throw new Exception(string.Format("Sec Type not processed:{0}", order.Security.SecType));

        }

        #endregion
        
        #region Public Methods

        public ValidateNewOrder ValidateNewOrder(Order order)
        {
            try
            {
           
                string url = string.Format("{0}{1}", BaseURL, _VALIDATE_NEW_ORDER_ASYNC);
                
                Dictionary<string, string> queryString =  new Dictionary<string, string>();
                queryString.Add("NombreEspecie", order.Security.Symbol);
                queryString.Add("Cantidad", order.OrderQty.HasValue ? order.OrderQty.Value.ToString() : "0");

                queryString.Add("Precio", order.Price.HasValue ? order.Price.Value.ToString() : "");
                queryString.Add("Importe",  "");

                if (order.Side == Side.Buy)
                    queryString.Add("OptionTipo", _SIDE_BUY);
                else if (order.Side == Side.Sell)
                    queryString.Add("OptionTipo", _SIDE_SELL);
                else
                    throw new Exception(string.Format("Side not implemented:{0}", order.Side));

                queryString.Add("DateValid", GetTIF(order));
                
                queryString.Add("OptionTipoPlazo", GetSettlement(order));
                
                string resp = DoPostJson(url, queryString);

                ValidateNewOrder validateOrdResp= JsonConvert.DeserializeObject<ValidateNewOrder>(resp);

                return validateOrdResp;

            }
            catch (Exception e)
            {
                return new ValidateNewOrder()
                {
                    Success = false,
                    Error = new TransactionError()
                    {
                        Codigo = 0,
                        Descripcion = string.Format("ERROR @ValidateNewOrder:{0}", e.Message)
                    }
                };
            }
        }

        #endregion
    }
}