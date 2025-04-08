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
using zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.DTO.Generic;
using zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.Generic;

namespace zHFT.MessageBasedFullMarketConnectivity.Nasini.ServiceLayer
{
    public class PrimaryRiskAPI : BaseRESTClient, IPortfolioManagementInterface
    {

        #region Protected Attributes

        protected string URL { get; set; }



        

        #endregion

        #region Protected Static Consts

        public string _AUTHENTICATE = "auth/getToken";


        public string _PORTF_POSITIONS = "rest/risk/position/getPositions/{0}";

        public string _ACCOUNT_REPORT = "rest/risk/accountReport/{0}";

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


        public GenericResponse GetPortfolio(string accNumber)
        {
            string fullUrl = $"{URL}{string.Format(_PORTF_POSITIONS, accNumber)}";
            Dictionary<string, string> headers = new Dictionary<string, string>();

            GenericResponse genResp = DoGetJson(fullUrl, headers);

            if (genResp.success)
            {
                var result = JsonConvert.DeserializeObject<GetPositionsResp>(genResp.respContent);
                result.success= true;
                return result;
            }
            else
            {
                    return genResp;
            }


        }

        public GenericResponse GetAccountReport(string accNumber)
        {
            string fullUrl = $"{URL}{string.Format(_ACCOUNT_REPORT, accNumber)}";
            Dictionary<string, string> headers = new Dictionary<string, string>();

            GenericResponse genResp = DoGetJson(fullUrl, headers);

            if (genResp.success)
            {
                var result = JsonConvert.DeserializeObject<GetAccountReportResp>(genResp.respContent);
                result.success = true;
                return result;
            }
            else
            {
                return genResp;
            }


        }

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
                        AccessToken = token;
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
