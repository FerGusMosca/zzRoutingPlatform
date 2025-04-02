using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedFullMarketConnectivity.Common;
using zHFT.InstructionBasedFullMarketConnectivity.ServiceLayer;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;

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

        public string _AUTHENTICATE = "auth/getToken";

        #endregion


        #region Constructors


        public PrimaryRiskAPI(string url, string login, string password, ILogger pLogger = null)
        {
            URL = url;
            Login = login;
            Password = password;
            Logger = pLogger;
        }

        #endregion

        #region Public Methods

        public GenericResponse Authenticate() // Generates the token and locally saves it
        {
            string fullUrl = $"{URL}{_AUTHENTICATE}";

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("X-Username", Login);
            headers.Add("X-Password", Password);

            var genResp = DoPostForm(fullUrl, headers);

            if (genResp.success)
            {
                DoLog($"==> Connecting to URL {fullUrl} w/user {Login} and pass {Password}");
                if (genResp.resp.Headers.TryGetValues("X-Auth-Token", out var values))
                {
                    DoLog($"Number of X-Auth-Token values: {values.Count()}");
                   
                    string token = values.FirstOrDefault();
                    if (!string.IsNullOrEmpty(token))
                    {
                        DoLog($"Successfully authenticated with Nasini REST :{token}", Constants.MessageType.PriorityInformation);
                        AuthToken = token;
                        return genResp;
                    }
                    else
                    {
                        DoLog("X-Auth-Token value is empty.");
                        return new GenericResponse { success = false, error = new GenericError { message = "X-Auth-Token is empty" } };
                    }
                }
                else
                {
                    DoLog("X-Auth-Token header not found.");
                    return new GenericResponse { success = false, error = new GenericError { message = "token header not found" } };
                }
            }
            else
            {
                return genResp;
            }
        }


        #endregion
    }
}
