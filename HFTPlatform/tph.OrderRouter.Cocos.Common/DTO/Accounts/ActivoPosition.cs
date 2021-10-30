namespace tph.OrderRouter.Cocos.Common.DTO.Accounts
{
    public class ActivoPosition
    {
        #region Public Attributes
        
        public string GTOS { get; set; }
        
        public string IMPOS { get; set; }
        
        public string ESPE { get; set; }
        
        public string TIPO { get; set; }
        
        public string Hora { get; set; }
        
        public SubtotalPosition[] Subtotal { get; set; }
        
        public int? CANT { get; set; }
        
        public string TCAM { get; set; }
        
        public string CAN2 { get; set; }
        
        
        
        #endregion
    }
}