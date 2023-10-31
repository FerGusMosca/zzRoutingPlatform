namespace OrderBookLoaderMock.Common.DTO
{
    public class HistoricalPricesUpdateDTO
    {
        #region Public Attributes
        
        public string ATSSymbol { get; set; }
        
        public string RealSymbol { get; set; }
        
        public int MaxTradingDays { get; set; }
        
        #endregion
    }
}