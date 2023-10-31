using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace tph.StrategyHandler.IBR.Cocos.ServiceLayer
{
    public class BaseServiceClient
    {
        #region Protected Consts

        protected string _LOGIN_URL = "/Login/Ingresar";

        protected string _VALIDATE_NEW_ORDER_ASYNC = "/Order/ValidarCargaOrdenAsync";

        #endregion
        
        #region Protected Attributes
        
        protected HttpClientHandler CookieHandler { get; set; }
        
        protected object tAuthLock { get; set;}
        
        protected string BaseURL { get; set; }
        
        public string DNI { get; set; }
        
        protected string User { get; set; }
        
        protected  string Password { get; set; }
        
        
        #endregion
        
        #region Constructors

        public BaseServiceClient()
        {
            tAuthLock=new object();
        }
    
        
        #endregion
    }
}