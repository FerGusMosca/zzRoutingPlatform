using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData
{
    public class SecurityListReqAckDTO : WebSocketMessage
    {
        #region Constructors

        public SecurityListReqAckDTO()
        {
            Msg = "SecurityListReqAck";
        }

        #endregion

        #region Public Attributes

        public string UUID { get; set; }

        public string ReqId { get; set; }

        public bool Success { get; set; }

        public string Error { get; set; }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return Success ? $" SecurityListReqAck successful! ReqId={ReqId} UUID={UUID}" : $"SecurityListReqAck error:{Error}  ReqId={ReqId} UUID={UUID}";
        }

        #endregion
    }
}
