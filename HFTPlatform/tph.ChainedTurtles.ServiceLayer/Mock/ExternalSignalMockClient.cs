using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using tph.ChainedTurtles.Common.DTO;
using tph.ChainedTurtles.Common.Interfaces;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using Formatting = Newtonsoft.Json.Formatting;

namespace tph.ChainedTurtles.ServiceLayer.Mock
{
    public class ExternalSignalMockClient : IExternalSignalClient
    {

        #region Protected Attributes

        protected Dictionary<string, string> ConfigDictionary { get; set; }

        protected OnLogMessage OnLogMessage { get; set; }

        protected ConcurrentQueue<string> MessagesQueue { get; set; }

        protected object tLock { get; set; }

        #endregion

        #region Public Methods

        public string EvalSignal(string featuresPayload)
        {

            BaseSignalResponseDTO mockResp = new BaseSignalResponseDTO()
            {
                action = BaseSignalResponseDTO._FLAT_ACTION,
                strategy = BaseSignalResponseDTO._MOCK_STRATEGY,
                timestamp = EpochConverter.ConvertToMilisecondEpoch(DateTime.Now)


            };

            string json = JsonConvert.SerializeObject(mockResp, Formatting.Indented);
            
            return json;
        }

        public ExternalSignalMockClient(Dictionary<string, string> pConfigDict, OnLogMessage pOnLogMessage)
        {
          

            ConfigDictionary = pConfigDict;
            OnLogMessage += pOnLogMessage;

            MessagesQueue = new ConcurrentQueue<string>();

            tLock = new object();

            //TODO load WebSocketURL

        }

        public bool Connect() { return true; }

        #endregion
    }
}
