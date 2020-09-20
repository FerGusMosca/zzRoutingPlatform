using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstrFullMarketConnectivity.IOL.BusinessEntities;
using zHFT.InstrFullMarketConnectivity.IOL.DataAccessLayer;
using zHFT.InstructionBasedMarketClient.IOL.Common.DTO;
using zHFT.Main.Common.Interfaces;

namespace zHFT.InstructionBasedMarketClient.IOL.DataAccessLayer
{
    public class IOLSecurityManager : BaseManager
    {
        #region Constructors

        public IOLSecurityManager(OnLogMessage OnLogMsg, string pMainURL, AccountInvertirOnlineData pAccountInvertirOnlineData )
        {

            Name = "Invertir Online Security List Client";
            Logger = OnLogMsg;
            AccountInvertirOnlineData = pAccountInvertirOnlineData;
           
            MainURL = pMainURL;
            
            //LoadConfig();
            Logger("Authenticating Security List Client On Invertir Online", Main.Common.Util.Constants.MessageType.Information);
            Authenticate();
            Logger(string.Format("Security List Client authenticated On Invertir Online. Token:{0}", AuthenticationToken.access_token),
                   Main.Common.Util.Constants.MessageType.Information);
        
        }

        #endregion

        #region Public Methods

        public  object GetSecurities(string country)
        {

            string iolExchange = _IOL_BYMA_EXCHANGE;


            string url = MainURL + string.Format(_SECURITIES_URL,country);
            try
            {
                string resp = DoGetJson(url);

                if (resp != null)
                {
                    object secList = JsonConvert.DeserializeObject<object>(resp);

                    return secList;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Se produjo un error accediendo al security list para el país {0}:{1}", country, ex.Message));
            }
        }

        public Fund[] GetFunds()
        {

            string iolExchange = _IOL_BYMA_EXCHANGE;


            string url = MainURL + string.Format(_FUNDS_URL);
            try
            {
                string resp = DoGetJson(url);

                if (resp != null)
                {
                    Fund[] fundArr = JsonConvert.DeserializeObject<Fund[]>(resp);

                    return fundArr;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Se produjo un error accediendo a la lista de FCI:{0}",  ex.Message));
            }
        }

        #endregion
    }
}
