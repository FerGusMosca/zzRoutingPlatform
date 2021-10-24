using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace tph.OrderRouter.ServiceLayer
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
        
        #region Protected Methods
        
        //if there are error, exceptions will be thrown
        protected void DoAuthenticate()
        {
            string url = string.Format("{0}{1}", BaseURL, _LOGIN_URL);
            
            Dictionary<string, string> queryString =  new Dictionary<string, string>();
            queryString.Add("Dni", DNI);
            queryString.Add("Usuario", User);
            queryString.Add("Password", Password);
            
            //CookieHandler

            if (CookieHandler == null)
            {
                CookieContainer cookies = new CookieContainer();
                CookieHandler = new HttpClientHandler();
                CookieHandler.CookieContainer = cookies;
            }

            string resp = DoPostJson(url, queryString);

            if (!resp.Contains("usuarioLogueado"))
                throw new Exception(string.Format("Could not authenticate user {0} and DNI {1}", User, DNI));

        }

        protected string DoPostJson(string url, IEnumerable<KeyValuePair<string, string>> postData)
        {

            lock (tAuthLock)
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 |
                                                       SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                
                using (var httpClient = new HttpClient(CookieHandler,false))
                {
                    using (var content = new FormUrlEncodedContent(postData))
                    {
                        content.Headers.Clear();
                        content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                        HttpResponseMessage response =  httpClient.PostAsync(url, content).Result;

                        string resp =  response.Content.ReadAsStringAsync().Result;

                        return resp;
                    }
                }
            }

        }

        #endregion
    }
}