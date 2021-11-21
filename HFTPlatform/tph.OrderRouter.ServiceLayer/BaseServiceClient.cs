using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using tph.OrderRouter.Cocos.Common.DTO.Generic;
using tph.OrderRouter.Cocos.Common.DTO.Orders;

namespace tph.OrderRouter.ServiceLayer
{
    public class BaseServiceClient
    {
        #region Protected Consts

        protected string _LOGIN_URL = "/Login/Ingresar";

        protected string _VALIDATE_NEW_ORDER_ASYNC = "/Order/ValidarCargaOrdenAsync";
        
        protected string _CONFIRM_NEW_ORDER_ASYNC = "/Order/EnviarOrdenConfirmadaAsyc";
        
        protected string _GET_POSITIONS = "/Consultas/GetConsulta";
        
        protected string _GET_EXECUTION_REPORTS = "/Consultas/GetConsulta";

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
        
        public  string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        //if there are error, exceptions will be thrown
        protected void DoAuthenticate()
        {
            string url = string.Format("{0}{1}", BaseURL, _LOGIN_URL);
            
            Dictionary<string, string> queryString =  new Dictionary<string, string>();
            queryString.Add("Dni", DNI);
            queryString.Add("Usuario", User);
            queryString.Add("Password", Password);
            //queryString.Add("IpAddress","190.193.48.235");
            queryString.Add("IpAddress",GetLocalIPAddress());
            
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
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.102 Safari/537.36");
                    //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                    httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
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