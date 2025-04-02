using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.Positions
{
    public class PortfolioReqAckDTO : WebSocketMessage
    {
        #region Constructors

        public PortfolioReqAckDTO()
        {
            Msg = "PortfolioReqAck";
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
            return Success ? $" PortfolioReqAck successful! ReqId={ReqId} UUID={UUID}" : $"HistoricalPricesReqAck error:{Error}  ReqId={ReqId} UUID={UUID}";
        }

        #endregion
    }
}
