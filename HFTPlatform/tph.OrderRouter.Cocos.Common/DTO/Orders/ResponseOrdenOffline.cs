namespace tph.OrderRouter.Cocos.Common.DTO.Orders
{
    public class ResponseOrdenOffline
    {
        #region Public Attributes
        
        public bool Accepted { get; set; }
        
        public string AcceptMessage { get; set; }
        
        public string ErrorMessage { get; set; }
        
        public bool Verified { get; set; }
        
        public bool HasWarnings { get; set; }
        
        public string[] WarningMessage { get; set; }
        
        public string Orden { get; set; }
        
        public bool HasReconfirmacion { get; set; }
        
        public string ReconfirmacionMessage { get; set; }
        
        public string ReconfirmacionTitulo { get; set; }
        
        
        
        #endregion
    }
}