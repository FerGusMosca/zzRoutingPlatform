using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.IOL.BusinessEntities;
using zHFT.InstructionBasedMarketClient.IOL.Common.DTO;
using zHFT.InstructionBasedMarketClient.IOL.DataAccessLayer.ADO;
using zHFT.Main.Common.Interfaces;

namespace zHFT.InstructionBasedMarketClient.IOL.DataAccessLayer
{
    public class IOLMarketDataManager
    {
        #region Constructors

        public IOLMarketDataManager(OnLogMessage OnLogMsg, int pAccountNumber, string pCredentialsConnectionString)
        {
          
            Logger = OnLogMsg;
            CredentialsConnectionString = pCredentialsConnectionString;
            AccountNumber = pAccountNumber;
            
            LoadConfig();
            Authenticate();
        
        }

        #endregion

        #region Private Consts

        private string _LOGIN_TOKEN_URL = "https://api.invertironline.com/token";

        private string _MARKET_DATA_URL = "https://api.invertironline.com/api/{Mercado}/Titulos/{Simbolo}/Cotizacion";

        private string _MAIN_BYMA_EXCHANGE = "BUE";

        private string _IOL_BYMA_EXCHANGE = "bCBA";

        private string _IOL_CLEAR_TPLUS2 = "t2";

        #endregion

        #region Protected Attributes

        protected OnLogMessage Logger { get; set; }

        protected string CredentialsConnectionString{ get; set; }

        protected int AccountNumber{ get; set; }

        protected AccountInvertirOnlineData AccountInvertirOnlineData { get; set; }

        protected AuthenticationToken AuthenticationToken { get; set; }

        #endregion

        #region Protected Methods

        protected void LoadConfig()
        {

            ADOAccountIOLDataManager accountIOLDataManager = new ADOAccountIOLDataManager(CredentialsConnectionString);

            AccountInvertirOnlineData = accountIOLDataManager.GetAccountPrimaryData(AccountNumber);

            if (AccountInvertirOnlineData == null)
                throw new Exception(string.Format("No se encontró la configuración de acceso al proveedor de datos Invertir Online para la cuenta número {0}", AccountNumber));

        }

        protected void Authenticate()
        {
            string url = _LOGIN_TOKEN_URL;
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
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Could not authenticate user {0} on Invertir Online", AccountInvertirOnlineData.User));
            }
        }

        protected string DoGetJson(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //request.Headers["X-Username"] = AccountPrimaryData.User;
            //request.Headers["X-Password"] = AccountPrimaryData.Password;
            //request.Headers["Authorization"] = "Bearer " + Convert.ToBase64String(Encoding.Default.GetBytes(string.Format("{0}:{1}", AccountPrimaryData.User, AccountPrimaryData.Password)));
            request.Headers["Authorization"] = "Bearer " + AuthenticationToken.access_token;


            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string content = string.Empty;
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    content = sr.ReadToEnd();
                }
            }
            return content;
        }


        #endregion

        #region Public Methods

        public MarketData GetMarketData(string symbol, string exchange)
        {

            string iolExchange = "";

            if (exchange == _MAIN_BYMA_EXCHANGE)
                iolExchange = _IOL_BYMA_EXCHANGE;
            else
                throw new Exception(string.Format("Mercado {0} no reconocido", exchange));


            string url = _MARKET_DATA_URL
                         + string.Format("?mercado=BCBA&simbolo={0}&model.simbolo={0}&model.mercado={1}&model.plazo={2}",
                                        symbol, iolExchange, _IOL_CLEAR_TPLUS2);
            try
            {
                string resp = DoGetJson(url);

                MarketData marketData = JsonConvert.DeserializeObject<MarketData>(resp);

                return marketData;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Se produjo un error accediendo al market data para el activo {0}:{1}", symbol, ex.Message));
            }
        }

        #endregion
    }
}
