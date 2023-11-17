using System;
using System.Collections.Generic;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.OrderImbSimpleCalculator.BusinessEntities
{
    public class MonSecurityImbalance:BaseMonSecurityImbalance
    {
        #region Constructors

        public MonSecurityImbalance(Security pSecurity, int? pDecimalRounding,string pCandelRefPrice,
                                    CustomImbalanceConfig pCustomImbalanceConfig,decimal pStopLoss)
        {
            Candles= new Dictionary<string, MarketData>();
            Security = pSecurity;
            DecimalRounding = pDecimalRounding;
            CandleReferencePrice = pCandelRefPrice;
            CustomImbalanceConfig = pCustomImbalanceConfig;


            //Turtles
            OpenWindow = 0;
            CloseWindow = pCustomImbalanceConfig.CloseWindow.HasValue ? pCustomImbalanceConfig.CloseWindow.Value : 0;
            StopLossForOpenPositionPct = Convert.ToDouble(pStopLoss);
            ExitOnMMov = pCustomImbalanceConfig.CloseMMov;
        }

        #endregion
        
        #region Protected Attributes
        
        public Dictionary<string, MarketData> Candles { get; set; }
        
        #endregion
        
        #region Protected Methods
        #endregion
        
        #region Public Overriden Methods
        
        #endregion
    }
}