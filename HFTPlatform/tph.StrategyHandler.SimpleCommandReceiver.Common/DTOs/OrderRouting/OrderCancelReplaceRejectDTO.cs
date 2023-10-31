namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class OrderCancelReplaceRejectDTO
    {
        #region Public Attributes
        
        public string ClOrdId { get; set; }
        
        public string OrigClOrdId { get; set; }
        
        public string Text { get; set; }
        
        public string Msg = "OrderCancelReplaceRejectMsg";
        
        #endregion
    }
}