using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;

namespace zHFT.InstructionBasedFullMarketConnectivity.ServiceLayer
{
    public class BaseRESTClient
    {
        #region Protected Attributes

       

        protected string AccessToken { get; set; }

        #endregion

        #region Private Methods

        protected GenericResponse DoInvoke(string url, Dictionary<string, string> headers, string body, HttpMethod method)
        {
            HttpRequestMessage request = CreateRequest(url, body, method);
            HttpResponseMessage response = null;

            try
            {
                var client = new HttpClient();
                response = client.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();
                return ProcessSuccessfulResponse(response);
            }
            catch (HttpRequestException ex)
            {
                return ProcessUnsuccessfulResponse(response, ex);
            }
            catch (Exception ex)
            {
                return new GenericResponse()
                {
                    success = false,
                    error =
                        new GenericError()
                        {
                            code = "0",
                            message = $"Critical error invoking Remote service @url {url} :{ex.Message}"
                        }
                };
            }
        }

        private GenericResponse ProcessSuccessfulResponse(HttpResponseMessage response)
        {
            return new GenericResponse() { success = true, resp = response };
        }

        private GenericResponse ProcessUnsuccessfulResponse(HttpResponseMessage response, HttpRequestException ex)
        {

            try
            {
                GenericResponse genResp = new GenericResponse() { success = false };

                genResp.error =
                    JsonConvert.DeserializeObject<GenericError>(response.Content
                        .ReadAsStringAsync().Result);

                return genResp;

            }
            catch (Exception e)
            {
                return new GenericResponse()
                {
                    success = false,
                    error =
                        new GenericError()
                        {
                            code = "0",
                            message = ex.Message
                        }
                };
            }

        }

        #endregion


        #region Protected Methods


        protected string ExtractId(HttpResponseMessage resp)
        {

            IEnumerable<string> values;

            if (resp.Headers.TryGetValues("Location", out values))
            {
                string url = values.FirstOrDefault<string>();


                string[] fields = url.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);


                if (fields.Length > 0)
                    return fields[fields.Length - 1];
                else
                {
                    throw new Exception($"Coudl not extract customer Id from wrongly formatted url {url}");
                }

            }
            else
            {
                throw new Exception($"Could not extract Location header from the response!");
            }

        }

        protected GenericResponse DoGetJson(string url, Dictionary<string, string> headers)
        {
            return DoInvoke(url, headers, null, HttpMethod.Get);
        }

        protected HttpRequestMessage CreateAuthFormRequest(string url, Dictionary<string, string> headers)
        {
            //string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(DwollaSetting.ClientId + ":" + DwollaSetting.Secret));
            string token = "";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", "Bearer " + token);

            var collection = new List<KeyValuePair<string, string>>();
            collection.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));

            var content = new FormUrlEncodedContent(collection);
            request.Content = content;

            return request;

        }

        protected HttpRequestMessage CreateRequest(string url, string body, HttpMethod method)
        {

            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("Authorization", "Bearer " + AccessToken);
            request.Headers.Add("Accept", "application/vnd.dwolla.v1.hal+json");
            //request.Headers.Add("Content-Type", "text/plain");

            if (body != null)
                request.Content = new StringContent(body, null, "application/json");

            return request;

        }


        protected GenericResponse ProcessException(Exception ex)
        {
            return new GenericResponse()
            {
                success = false,
                error =
                    new GenericError()
                    {
                        code = "0",
                        message = $"Critical error invoking Remote service :{ex.Message}"
                    }
            };
        }

        protected GenericResponse DoPostForm(string url, Dictionary<string, string> headers)
        {
            HttpRequestMessage request = CreateAuthFormRequest(url, headers);
            HttpResponseMessage response = null;
            try
            {
                var client = new HttpClient();
                response = client.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();


                return ProcessSuccessfulResponse(response);
            }
            catch (HttpRequestException ex)
            {
                return ProcessUnsuccessfulResponse(response, ex);
            }
            catch (Exception ex)
            {
                return new GenericResponse()
                {
                    success = false,
                    error =
                        new GenericError()
                        {
                            code = "0",
                            message = $"Critical error invoking Dwolla service @url {url} :{ex.Message}"
                        }
                };
            }
        }

        protected GenericResponse DoPost(string url, string json)
        {
            return DoInvoke(url, new Dictionary<string, string>(), json, HttpMethod.Post);
        }

        #endregion
    }
}
