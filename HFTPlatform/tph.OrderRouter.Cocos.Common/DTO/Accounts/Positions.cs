using tph.OrderRouter.Cocos.Common.DTO.Generic;

namespace tph.OrderRouter.Cocos.Common.DTO.Accounts
{
    public class Positions:TransactionResponse
    {
        #region Public Attributes
        
        public PositionsResult Result { get; set; }
        
        #endregion
    }
}