using System.Collections.Generic;
using System.Security;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace tph.InstructionBasedMarketClient.IB.Common.DTO
{
    public class HistoricalPricesHoldingDTO
    {
        
        #region Constructors

        public HistoricalPricesHoldingDTO(int pReqId,string pSymbol, string pCurrency, SecurityType pSecType)
        {
            ReqId = pReqId;
            Security = new Security() {Symbol = pSymbol, Currency = pCurrency, SecType = pSecType};
            MarketDataList = new List<MarketData>();

        }

        #endregion
        
        #region  Public Attributes

        public  int ReqId { get; set; }
        
        public  Security Security { get; set; }
        
        public  List<MarketData> MarketDataList { get; set; }
        
        

        #endregion
    }
}