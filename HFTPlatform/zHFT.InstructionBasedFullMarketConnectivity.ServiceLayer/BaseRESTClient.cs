using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Interfaces;
using static zHFT.Main.Common.Util.Constants;

namespace zHFT.InstructionBasedFullMarketConnectivity.ServiceLayer
{
    public class BaseRESTClient
    {
        #region Protected Attributes

        protected ILogger Logger { get; set; }

        protected string AccessToken { get; set; }

        protected string Login { get; set; }

        protected string Password { get; set; }

        #endregion

        #region Private Methods

        protected void DoLog(string msg, MessageType type=MessageType.Information) {
        
            if(Logger!=null)
                Logger.DoLog(msg, type);
            else
                Console.WriteLine(msg);
        
        }

        protected GenericResponse DoInvokeWithBasic(string url, Dictionary<string, string> headers, string body, HttpMethod method)
        {
            //HttpRequestMessage request = CreateRequestAccessToken(url, body, method);
            HttpRequestMessage request = CreateRequestBasicAuth(url, body, method, Login, Password);

            try
            {
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true // Don't follow redirects automatically (e.g. to login page)
                };

                using (var client = new HttpClient(handler))
                {
                    using (var response = client.SendAsync(request).Result)
                    {
                        // Debug: Print status and headers
                        if (response.IsSuccessStatusCode)
                        {
                            // Success: process the successful response
                            string responseBody = response.Content.ReadAsStringAsync().Result;
                            return ProcessSuccessfulResponse(response,responseBody);
                        }
                        else
                        {
                            // Read the body BEFORE the response is disposed
                            string errorContent = response.Content.ReadAsStringAsync().Result;

                            // Handle non-successful response
                            return ProcessUnsuccessfulResponse(errorContent);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback for unexpected exceptions
                return new GenericResponse()
                {
                    success = false,
                    error = new GenericError()
                    {
                        code = "0",
                        message = $"Critical error invoking Remote service @url {url} : {ex.Message}"
                    }
                };
            }
        }


        private GenericResponse ProcessSuccessfulResponse(HttpResponseMessage response, string respContent)
        {
            return new GenericResponse() { success = true, resp = response, respContent = respContent };
        }

        private GenericResponse ProcessUnsuccessfulResponse(string errorContent)
        {

            GenericResponse genResp = new GenericResponse() { success = false, error = new GenericError() { message = errorContent } };
            return genResp;
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
            return DoInvokeWithBasic(url, headers, null, HttpMethod.Get);
        }

        protected HttpRequestMessage CreateAuthFormRequest(string url, Dictionary<string, string> headers)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Add the headers directly to the request
            foreach (string key in headers.Keys)
            {
                request.Headers.Add(key, headers[key]);
            }

            // Do not set a body (mimic Postman's empty body)
            // request.Content = null; // This is the default, so we can omit setting it

            return request;
        }

        //For future references
        //protected void EvalRedirectionError(HttpResponseMessage response)
        //{
        //    // Check for redirect status codes
        //    if (response.StatusCode == System.Net.HttpStatusCode.Redirect ||
        //        response.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
        //        response.StatusCode == System.Net.HttpStatusCode.TemporaryRedirect ||
        //        response.StatusCode == System.Net.HttpStatusCode.SeeOther)
        //    {
        //        var location = response.Headers.Location;
        //        DoLog($"Redirect detected! Location: {location}",MessageType.Error);

        //        // Check for X-Auth-Token in the redirect response
        //        DoLog("Checking response.Headers in redirect response:");
        //        bool foundInHeaders = false;
        //        string authToken = null;
        //        foreach (var header in response.Headers)
        //        {
        //            DoLog($"{header.Key}: {string.Join(", ", header.Value)}");
        //            if (string.Equals(header.Key, "X-Auth-Token", StringComparison.OrdinalIgnoreCase))
        //            {
        //                DoLog("Found X-Auth-Token in response.Headers of redirect response!");
        //                authToken = header.Value.FirstOrDefault();
        //                foundInHeaders = true;
        //            }
        //        }

        //        if (!foundInHeaders)
        //        {
        //            DoLog("Checking response.Content.Headers in redirect response:");
        //            foreach (var header in response.Content.Headers)
        //            {
        //                DoLog($"{header.Key}: {string.Join(", ", header.Value)}");
        //                if (string.Equals(header.Key, "X-Auth-Token", StringComparison.OrdinalIgnoreCase))
        //                {
        //                    DoLog("Found X-Auth-Token in response.Content.Headers of redirect response!");
        //                    authToken = header.Value.FirstOrDefault();
        //                }
        //            }
        //        }


        //        if (foundInHeaders && !string.IsNullOrEmpty(authToken))
        //        {
        //            return;
        //        }
        //        else
        //        {
        //            throw new Exception($"Redirect detected to {location}. X-Auth-Token not found in initial response.");
        //        }
        //    }
        //}

        protected HttpRequestMessage CreateRequestAccessToken(string url, string body, HttpMethod method)
        {

            var request = new HttpRequestMessage(method, url);
            if (AccessToken != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            else
                throw new Exception($"Not authenticated with {url} !!");

            request.Headers.Add("User-Agent", "PostmanRuntime/7.43.3");
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("Connection", "keep-alive");

            if (body != null)
                request.Content = new StringContent(body, null, "application/json");


            return request;

        }

        protected HttpRequestMessage CreateRequestBasicAuth(string url, string body, HttpMethod method, string username = null, string password = null)
        {
            var request = new HttpRequestMessage(method, url);

            // Basic Authentication header
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }
            else
            {
                throw new Exception($"Username and password must be provided for Basic Authentication ({url})");
            }

            // Headers (copied from Postman)
            request.Headers.Add("User-Agent", "PostmanRuntime/7.43.3");
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("Connection", "keep-alive");

            // Body (if present)
            if (body != null)
            {
                request.Content = new StringContent(body, null, "application/json");
            }

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
                // Create HttpClient with redirect handling disabled
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false
                };
                var client = new HttpClient(handler);

                // Set headers to match Postman
                request.Headers.Add("User-Agent", "PostmanRuntime/7.43.3");
                request.Headers.Add("Accept", "*/*");
                request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                request.Headers.Add("Connection", "keep-alive");

                // Send the request synchronously
                response = client.SendAsync(request).Result;

                // Log the response status code
                DoLog($"Response Status Code: {(int)response.StatusCode} {response.StatusCode}");


                // If not a redirect, ensure the response is successful
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;
                return ProcessSuccessfulResponse(response, responseBody);
            }
            catch (HttpRequestException ex)
            {
                string errorContent = "";

                if (response != null && response.Content != null)
                {
                    try
                    {
                        errorContent = response.Content.ReadAsStringAsync().Result;
                    }
                    catch (ObjectDisposedException) { /* ignora si ya se cerró */ }
                }


                return ProcessUnsuccessfulResponse(errorContent);
            }
            catch (Exception ex)
            {
                return new GenericResponse
                {
                    success = false,
                    error = new GenericError
                    {
                        code = "0",
                        message = $"Critical error invoking Dwolla service @url {url} :{ex.Message}"
                    }
                };
            }
            finally
            {
                response?.Dispose();
            }
        }

        protected GenericResponse DoPost(string url, string json)
        {
            return DoInvokeWithBasic(url, new Dictionary<string, string>(), json, HttpMethod.Post);
        }

        #endregion
    }
}
