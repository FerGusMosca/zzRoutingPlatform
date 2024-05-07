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
            //OpenWindow = 0;
            //CloseWindow = pCustomImbalanceConfig.CloseWindow.HasValue ? pCustomImbalanceConfig.CloseWindow.Value : 0;
            StopLossForOpenPositionPct = Convert.ToDouble(pStopLoss);
            //ExitOnMMov = pCustomImbalanceConfig.CloseMMov;

            ValidateConfig();
        }

        #endregion
  

        #region Protected Methods

        public void ValidateConfig()
        {
            if (CustomImbalanceConfig.CloseMMov)
            {
                if (!CustomImbalanceConfig.CloseWindow.HasValue || CustomImbalanceConfig.CloseWindow <= 0)
                    throw new Exception($"CloseMMov=true and no Close Window specified for symbol ={Security.Symbol}!!!");
            
            }
            else if (CustomImbalanceConfig.CloseTurtles)
            {
                if (!CustomImbalanceConfig.CloseWindow.HasValue || CustomImbalanceConfig.CloseWindow <= 0)
                    throw new Exception($"CloseTurtles=true and no Close Window specified for symbol ={Security.Symbol}!!!");

            }
            else if (CustomImbalanceConfig.CloseOnImbalance)
            {
                if (!CustomImbalanceConfig.CloseImbalance.HasValue || CustomImbalanceConfig.CloseImbalance <= 0)
                    throw new Exception($"CloseOnImbalance=true and no Close Imbalance specified for symbol ={Security.Symbol}!!!");

            }

        }

        #endregion
        
        #region Public Overriden Methods
        
        #endregion
    }
}