using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using zHFT.OrderRouters.Bittrex.Common.Dto;

namespace tph.OrderRouter.Bittrex.ServiceLayer
{
    public class BaseClient
    {

        #region Protected Attributes
        
        protected string BaseURL { get; set; }
        
        protected  string ApiKey { get; set; }
        
        protected  string ApiSecret { get; set; }
        
        #endregion
        
        #region Protected Methods
        
        public  string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        
        protected  string SHA512(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            using (var hash = System.Security.Cryptography.SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);

                return ByteArrayToString(hashedInputBytes);

//                // Convert to text
//                // StringBuilder Capacity is 128, because 512 bits / 8 bits in byte * 2 symbols for byte 
//                var hashedInputStringBuilder = new System.Text.StringBuilder(128);
//                foreach (var b in hashedInputBytes)
//                    hashedInputStringBuilder.Append(b.ToString("X2"));
//                return hashedInputStringBuilder.ToString();
            }
        }

        protected void CreateHeaders(HttpWebRequest request,string body)
        {

            request.Headers["Api-Key"] = ApiKey;
            request.Headers["Api-Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();


            if (body != null)
            {
                request.Headers["Api-Content-Hash"] = SHA512(body);
            }
            else
            {
                request.Headers["Api-Content-Hash"] = SHA512("");
            }
            
            //Api-Signature
        }

        protected GenericResponse DoPostJsonResponse(string url, string body)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "POST";
            request.ContentType = "application/json";

//            foreach (string key in headers.Keys)
//            {
//                request.Headers[key] = headers[key];
//            }

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(body);
            }
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                
                string content = string.Empty;
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        content = sr.ReadToEnd();
                    }
                }
                GenericResponse resp = JsonConvert.DeserializeObject<GenericResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                resp.Resp = content;

                return resp;
            }
            catch (WebException ex)
            {
                
                string errContent = string.Empty;

                if (ex.Response != null)
                {


                    using (Stream stream = ex.Response.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(stream))
                        {
                            errContent = sr.ReadToEnd();
                        }
                    }
                }
                else
                    errContent = ex.Message;

                GenericResponse errResp = JsonConvert.DeserializeObject<GenericResponse>(errContent, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                return errResp;
            }
            catch (Exception ex)
            {
                return new GenericResponse() { Success = false, Error = ex.Message };
            }
        }
        
        #endregion
    }
}