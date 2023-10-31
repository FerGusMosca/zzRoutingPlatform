using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO;
using zHFT.InstructionBasedMarketClient.BitMex.DAL.API;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.InstructionBasedMarketClient.BitMex.DAL.REST
{
    public class SecurityListManager : BaseManager
    {
        #region Protected Attributes

        public string URL { get; set; }

        #endregion

        #region Private Static Consts

        private static string _INSTRUMENTS = "/instrument";

        private static string _INSTRUMENTS_GET_ACTIVE = "/instrument/active";

        private static string _INSTRUMENTS_AND_INDICES_GET_ACTIVE = "/instrument/activeAndIndices";

        #endregion


        #region Constructors

        public SecurityListManager(string url)
        {
            URL = url;

        }

        #endregion

        #region Public Methods

        public List<zHFT.InstructionBasedMarketClient.BitMex.BE.Security> GetActiveSecurityList()
        {
            List<zHFT.InstructionBasedMarketClient.BitMex.BE.Security> securities = new List<zHFT.InstructionBasedMarketClient.BitMex.BE.Security>();

            BitMEXApi api = new BitMEXApi(URL);

            var param = new Dictionary<string, string>();
            string resp = api.Query("GET", _INSTRUMENTS_GET_ACTIVE, param, false);

            Instrument[] instrArr = JsonConvert.DeserializeObject<Instrument[]>(resp);

            foreach (Instrument instr in instrArr)
                securities.Add(MapSecurity(instr));

            return securities;

        }

        #endregion

    }
}
