using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.InstrFullMarketConnectivity.IOL.BusinessEntities;
using zHFT.InstrFullMarketConnectivity.IOL.Common.DTO;
using zHFT.InstrFullMarketConnectivity.IOL.DataAccessLayer.ADO;
using zHFT.Main.Common.Interfaces;

namespace zHFT.InstrFullMarketConnectivity.IOL.DataAccessLayer
{
    public class BaseManager
    {
        #region Protected Consts

        protected string _LOGIN_TOKEN_URL = "token";

        protected string _MARKET_DATA_URL = "api/{Mercado}/Titulos/{Simbolo}/Cotizacion";

        protected string _BUY_URL = "api/operar/Comprar";

        protected string _SELL_URL = "api/operar/Vender";

        protected string _MAIN_BYMA_EXCHANGE = "BUE";

        protected string _IOL_BYMA_EXCHANGE = "bCBA";

        protected string _IOL_CLEAR_TPLUS2 = "t2";

        #endregion

        #region Protected Attributes

        protected string Name { get; set; }

        protected OnLogMessage Logger { get; set; }

        protected string CredentialsConnectionString { get; set; }

        protected int AccountNumber { get; set; }

        protected string MainURL { get; set; }

        protected AccountInvertirOnlineData AccountInvertirOnlineData { get; set; }

        protected AuthenticationToken AuthenticationToken { get; set; }

        protected object tAuthLock = new object();

        protected Thread RefreshTokenThread { get; set; }


        #endregion

        #region Protected Methods

        protected void Authenticate()
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            string url = MainURL + _LOGIN_TOKEN_URL;
            string postData = string.Format("username={0}&password={1}&grant_type=password", AccountInvertirOnlineData.User, AccountInvertirOnlineData.Password);
            var data = Encoding.ASCII.GetBytes(postData);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            try
            {

                var response = (HttpWebResponse)request.GetResponse();

                var resp = new StreamReader(response.GetResponseStream()).ReadToEnd();

                AuthenticationToken = JsonConvert.DeserializeObject<AuthenticationToken>(resp);

                RefreshTokenThread = new Thread(RefreshToken);
                RefreshTokenThread.Start();

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Could not authenticate user {0} on Invertir Online", AccountInvertirOnlineData.User));
            }
        }

        protected void LoadConfig()
        {

            ADOAccountIOLDataManager accountIOLDataManager = new ADOAccountIOLDataManager(CredentialsConnectionString);

            AccountInvertirOnlineData = accountIOLDataManager.GetAccountPrimaryData(AccountNumber);

            if (AccountInvertirOnlineData == null)
                throw new Exception(string.Format("No se encontró la configuración de acceso al proveedor de datos Invertir Online para la cuenta número {0}", AccountNumber));

        }

        protected string DoGetJson(string url)
        {
            string content = string.Empty;
            lock (tAuthLock)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                //request.Headers["X-Username"] = AccountPrimaryData.User;
                //request.Headers["X-Password"] = AccountPrimaryData.Password;
                //request.Headers["Authorization"] = "Bearer " + Convert.ToBase64String(Encoding.Default.GetBytes(string.Format("{0}:{1}", AccountPrimaryData.User, AccountPrimaryData.Password)));

                if (AuthenticationToken != null)
                    request.Headers["Authorization"] = "Bearer " + AuthenticationToken.access_token;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        content = sr.ReadToEnd();
                    }
                }
            }
            return content;
        }

        protected string DoPostJson(string url, Dictionary<string, string> queryStrings)
        {
            string content = string.Empty;

            lock (tAuthLock)
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                string postData = "";
                foreach (string key in queryStrings.Keys)
                {
                    postData += key + "=" + queryStrings[key] + "&";
                }
                var data = Encoding.ASCII.GetBytes(postData);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;

                if (AuthenticationToken != null)
                    request.Headers["Authorization"] = "Bearer " + AuthenticationToken.access_token;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        content = sr.ReadToEnd();
                    }
                }
            }
            return content;
        }

        protected void RefreshToken(object param)
        {

            while (true)
            {
                lock (tAuthLock)
                {
                    try
                    {
                        string url = MainURL + _LOGIN_TOKEN_URL;
                        string postData = string.Format("refresh_token={0}&grant_type=refresh_token", AuthenticationToken.refresh_token);
                        var data = Encoding.ASCII.GetBytes(postData);

                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.Method = "POST";
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.ContentLength = data.Length;

                        using (var stream = request.GetRequestStream())
                        {
                            stream.Write(data, 0, data.Length);
                        }

                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        string content = "";
                        using (Stream stream = response.GetResponseStream())
                        {
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                content = sr.ReadToEnd();
                            }
                        }

                        AuthenticationToken = JsonConvert.DeserializeObject<AuthenticationToken>(content);

                        Logger(string.Format("Actualización del refresh token @{0}:{1}", Name, AuthenticationToken.refresh_token), zHFT.Main.Common.Util.Constants.MessageType.Information);

                    }
                    catch (Exception ex)
                    {
                        Logger(string.Format("Error invocando el servicio de refresh token:{0}", ex.Message),zHFT.Main.Common.Util.Constants.MessageType.Error);
                    }
                }
                Thread.Sleep(1000 * 60);//Once per minute
            }
        
        }

        #endregion
    }
}
