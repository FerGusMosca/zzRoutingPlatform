using tph.OrderRouter.Cocos.Common.DTO.Generic;

namespace tph.OrderRouter.Cocos.Common.DTO.Orders
{
    public class ValidateNewOrder:TransactionResponse
    {
        #region Public Attributes
        
        public ValidateNewOrderResult Result { get; set; }
        
        #endregion
        
    }
}