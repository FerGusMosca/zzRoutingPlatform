namespace tph.OrderRouter.Cocos.Common.DTO.Orders
{
    public class ExecutionReport
    {
        #region Public Attributes
        
        public string especie { get; set; }
        
        public string totalcantCompra { get; set; }
        
        public string totalcantVenta { get; set; }
        
        public string totalImpoCompraPesos { get; set; }
        
        public string totalImpoCompraDolar { get; set; }
        
        public string totalImpoCompraDolarC { get; set; }
        
        public string totalImpoVentaDolarC { get; set; }
        
        public string totalImpoVentaPesos { get; set; }
        
        public string totalImpoVentaDolar { get; set; }
        
        public ListaDetalleTicker[] listaDetalleTiker { get; set; }
        
        #endregion
    }
}