using System;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class OrderMassStatusRequestAck:WebSocketMessage
    {
        #region Constructors

        public OrderMassStatusRequestAck()
        {
            Msg = "OrderMassStatusRequestAck";
        }

        #endregion
        
        #region Public Attributes

        public bool Success { get; set; }

        public string Error { get; set; }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return Success ? $" OrderMassStatusRequestAck successful!" : $"OrderMassStatusRequestAck error:{Error} ";
        }

        #endregion
    }
}