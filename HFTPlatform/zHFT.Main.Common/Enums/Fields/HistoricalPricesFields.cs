namespace zHFT.Main.Common.Enums
{
    public class HistoricalPricesFields: Fields
    {
        
        public static readonly HistoricalPricesFields Security = new HistoricalPricesFields(2);
        
        public static readonly HistoricalPricesFields Candles = new HistoricalPricesFields(3);
        
        protected HistoricalPricesFields(int pInternalValue) : base(pInternalValue)
        {
        
        }
    }
}