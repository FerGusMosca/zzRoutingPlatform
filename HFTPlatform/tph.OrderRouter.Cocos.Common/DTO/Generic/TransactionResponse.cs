namespace tph.OrderRouter.Cocos.Common.DTO.Generic
{
    public class TransactionResponse
    {
        #region Public Attributes
        
        public bool Success { get; set; }
        
        public TransactionError Error { get; set; }

        #endregion
    }
}