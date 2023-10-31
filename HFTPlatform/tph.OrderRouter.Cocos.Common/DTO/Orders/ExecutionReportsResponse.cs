using tph.OrderRouter.Cocos.Common.DTO.Generic;

namespace tph.OrderRouter.Cocos.Common.DTO.Orders
{
    public class ExecutionReportsResponse:TransactionResponse
    {
        #region Public Attributes
        
        public ExecutionReport[] Result { get; set; }
        
        #endregion
    }
}