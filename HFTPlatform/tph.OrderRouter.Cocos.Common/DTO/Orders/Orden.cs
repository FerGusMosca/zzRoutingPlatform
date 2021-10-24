namespace tph.OrderRouter.Cocos.Common.DTO.Orders
{
    public class Orden
    {
        #region Public Attriibutes
        
        public string Ticker { get; set; }
        
        public int NroOrden { get; set; }
        
        public bool Anula { get; set; }
        
        public bool IsJson { get; set; }
        
        public bool Verified { get; set; }
        
        public string Estado { get; set; }
        
        public string Market { get; set; }
        
        public string HorarioOperatoria { get; set; }
        
        public bool OrdenConValidez { get; set; }
        
        public string FeVto { get; set; }
        
        public bool Disparo { get; set; }
        
        public string Hao { get; set; }
        
        public string UserId { get; set; }
        
        public string Origen { get; set; }
        
        public string TOper { get; set; }
        
        public int Reconfirmacion { get; set; }
        
        public string Especie { get; set; }
        
        public string Comprobante { get; set; }
        
        public string EstadoDevuelto { get; set; }
        
        public int Comitente { get; set; }
        
        public int Cantidad { get; set; }
        
        public string DivulgacionParcial { get; set; }
        
        public decimal Precio { get; set; }
        
        public decimal Importe { get; set; }
        
        public string Dni { get; set; }
        
        public bool EsCompra { get; set; }
        
        public string Mger { get; set; }
        
        public int Tipo { get; set; }
        
        public string Nro { get; set; }
        
        #endregion
    }
}