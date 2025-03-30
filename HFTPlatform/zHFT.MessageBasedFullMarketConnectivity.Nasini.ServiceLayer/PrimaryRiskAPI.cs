using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedFullMarketConnectivity.Common;
using zHFT.InstructionBasedFullMarketConnectivity.ServiceLayer;
using zHFT.Main.Common.DTO;

namespace zHFT.MessageBasedFullMarketConnectivity.Nasini.ServiceLayer
{
    public class PrimaryRiskAPI : BaseRESTClient, IPortfolioManagementInterface
    {

        #region Protected Attributes

        protected string URL { get; set; }

        protected string Login { get; set; }

        protected string Password { get; set; }

        protected string AuthToken { get; set; }

        #endregion

        #region Protected Static Consts

        public string _AUTHENTICATE = "/auth/getToken";

        #endregion


        #region Constructors


        public PrimaryRiskAPI(string url, string login, string password)
        {
            URL = url;
            Login = login;
            Password = password;
        }

        #endregion

        #region Public Methods

        public GenericResponse Authenticate()//Generates the token and locally saves it
        {
            string fullUrl = $"{URL}{_AUTHENTICATE}";

            Dictionary<string, string> headers=new Dictionary<string, string>();


            headers.Add("X-Username", Login);
            headers.Add("X-Password", Password);

            var genResp=DoPostForm(fullUrl, headers);

            if (genResp.success)
            {
                foreach (var header in genResp.resp.Headers)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }

                // Attempt to get the "X-Auth-Token" header from the response headers
                if (genResp.resp.Headers.TryGetValues("X-Auth-Token", out var values))
                {
                    // Get the first token value (in case there are multiple)
                    string token = values.FirstOrDefault();
                    AuthToken=token;
                    return genResp;
                }
                else
                {
                    return new GenericResponse() { success = false, error = new GenericError() { message = $"Could not authenticate user {Login} on server {URL}" } };
                }
            }
            else

                return genResp;

        }


        #endregion
    }
}
