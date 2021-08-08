namespace zHFT.Main.Common.Enums
{
    public class HistoricalPricesRequestFields: Fields
    {
        public static readonly Fields Symbol = new HistoricalPricesRequestFields(2);

        
        public static readonly HistoricalPricesRequestFields From = new HistoricalPricesRequestFields(3);
        public static readonly HistoricalPricesRequestFields To = new HistoricalPricesRequestFields(4);
        public static readonly HistoricalPricesRequestFields Interval = new HistoricalPricesRequestFields(5);
        public static readonly HistoricalPricesRequestFields MDReqId = new HistoricalPricesRequestFields(6);
        
        protected HistoricalPricesRequestFields(int pInternalValue)
            : base(pInternalValue)
        {

        }
    }
}