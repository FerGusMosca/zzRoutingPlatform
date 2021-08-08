using System.Collections.Generic;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.InstructionBasedMarketClient.Binance.Common.Wrappers
{
    public class HistoricalPricesWrapper:Wrapper
    {
        
        #region Constructors

        public HistoricalPricesWrapper(List<Wrapper> candlesWrapper)
        {
            CandlesWrapper = candlesWrapper;
        }

        #endregion
        
        #region Protected Attributes
        
        protected List<Wrapper> CandlesWrapper { get; set; }
        
        #endregion
        
        #region Public Methods
        public override object GetField(Fields field)
        {
            return CandlesWrapper;
        }

        public override Actions GetAction()
        {
            return Actions.HISTORICAL_PRICES;
        }
        
        #endregion
    }
}