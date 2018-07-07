using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstrFullMarketConnectivity.IOL.DataAccessLayer;
using zHFT.InstrFullMarketConnectivity.IOL.DataAccessLayer.ADO;
using zHFT.InstructionBasedMarketClient.IOL.Common.DTO;
using zHFT.Main.Common.Interfaces;

namespace zHFT.InstructionBasedMarketClient.IOL.DataAccessLayer
{
    public class IOLMarketDataManager:BaseManager
    {
        #region Constructors

        public IOLMarketDataManager(OnLogMessage OnLogMsg, int pAccountNumber, 
                                    string pCredentialsConnectionString,string pMainURL)
        {
          
            Logger = OnLogMsg;
            CredentialsConnectionString = pCredentialsConnectionString;
            AccountNumber = pAccountNumber;
            MainURL = pMainURL;
            
            LoadConfig();
            Logger("Authenticating Market Data Client On Invertir Online", Main.Common.Util.Constants.MessageType.Information);
            Authenticate();
            Logger(string.Format("Market Data Client authenticated On Invertir Online. Token:{0}", AuthenticationToken.access_token),
                   Main.Common.Util.Constants.MessageType.Information);
        
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


            string url = MainURL + _MARKET_DATA_URL
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
