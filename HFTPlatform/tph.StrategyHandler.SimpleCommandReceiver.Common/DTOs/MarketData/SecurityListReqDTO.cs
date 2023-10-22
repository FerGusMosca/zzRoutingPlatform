using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData
{
    public class SecurityListReqDTO : WebSocketMessage
    {
        #region Constructors

        public SecurityListReqDTO()
        {
            Msg = "SecurityListRequest";
        }

        #endregion

        #region Public Attributes

        public string Symbol { get; set; }

        public SecurityListRequestType SecurityListRequestType { get; set; }

        public SecurityType SecurityType { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }

        #endregion
    }
}
