using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace PrimaryCertification
{
    public class PositionsServiceLayer
    {
        #region Constructors

        public PositionsServiceLayer(string pUrl,string pUsername, string pPassword)
        {
            URL = pUrl;
            Username = pUsername;
            Password = pPassword;

        }

        #endregion
        
        #region Protected Atrributes
        
        protected string URL { get; set; }
        
        protected string Username { get; set; }
        
        protected string Password { get; set; }
        
        #endregion
        
        #region Private Static Consts

        private string _POSITIONS_URL = "/oms-webservices/rest/detailedPosition/";
        
        #endregion
        
        #region Private Methods
        
        protected string DoGetJson(string url)
        {
            string content = string.Empty;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //request.Headers["X-Username"] = AccountPrimaryData.User;
            //request.Headers["X-Password"] = AccountPrimaryData.Password;
            //request.Headers["Authorization"] = "Bearer " + Convert.ToBase64String(Encoding.Default.GetBytes(string.Format("{0}:{1}", AccountPrimaryData.User, AccountPrimaryData.Password)));

            if (Username != null && Password != null)
            {
                String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(Username + ":" + Password));
                request.Headers.Add("Authorization", "Basic "+ encoded);
            }
            
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

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

        public PositionsDto GetPositions(string account)
        {

            string fullUrl = URL + _POSITIONS_URL + account;


            string resp = DoGetJson(fullUrl);
            
            PositionsDto respDTO = JsonConvert.DeserializeObject<PositionsDto>(resp);

            return respDTO;
        }

        #endregion
    }
}