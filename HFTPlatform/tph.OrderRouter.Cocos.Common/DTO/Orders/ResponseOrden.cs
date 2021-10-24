namespace tph.OrderRouter.Cocos.Common.DTO.Orders
{
    public class ResponseOrden
    {
        #region Public Attributes
        
        public bool Accepted { get; set; }

        public string AcceptMessage { get; set; }
        
        public string ErrorMessage { get; set; }
        
        public bool Verified { get; set; }
        
        public Orden Orden { get; set;}
        
        public bool HasWarnings { get; set; }
        
        public string[]   WarningMessage { get; set; }
        
        public bool HasReconfirmacion { get; set; }
        
        public string ReconfirmacionMessage { get; set; }
        
        public string ReconfirmacionTitulo { get; set; }
        
        public ResponseOrdenOffline ResponseOrdenOffline { get; set; }
        
        public bool OperaContado { get; set; }
        
        public bool EsCarga { get; set; }
        
        public bool EsVerificacion { get; set; }
        
        public bool EsConfirmacion { get; set; }
        
        public bool EsReenvio { get; set; }
        
        public bool EsCancelacion { get; set; }
        
        public bool EsVerificacionCancelacion { get; set; }
        
        public bool EsConfirmacionCancelacion { get; set; }
        
        public bool EstadoAnulacion { get; set; }
        
        public string Especie { get; set; }
        
        public string FeVtoFormat { get; set; }
        
        public int Cantidad { get; set; }
        
        public decimal Precio { get; set; }
        
        public string Importe { get; set; }
        
        public bool OrdenConValidez { get; set; }
        
        #endregion
        
    }
}