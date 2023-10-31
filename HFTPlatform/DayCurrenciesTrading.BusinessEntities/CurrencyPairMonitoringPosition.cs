using System;
using DayCurrenciesTrading.BusinessEntities.TechnicalIndicators;
using DayCurrenciesTrading.Common.Configuration;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace DayCurrenciesTrading.BusinessEntities
{
    public class CurrencyPairMonitoringPosition
    {
        
        #region Constructors

        public CurrencyPairMonitoringPosition(Security pPair, Configuration pConfiguration)
        {
            Pair = pPair;
            ExponentialMovingAverageLong=new ExponentialMovingAverage(pConfiguration.MovAvgLong);
            ExponentialMovingAverageShort=new ExponentialMovingAverage(pConfiguration.MovAvgShort);
            CLosing = false;
        }

        #endregion
        
        #region Public Attributes
        
        public Security Pair { get; set; }
        
        public ExponentialMovingAverage ExponentialMovingAverageLong { get; set; }
        
        public ExponentialMovingAverage ExponentialMovingAverageShort { get; set; }
        
        public Position LastRoutingOpeningPosition { get; set; }
        
        public Position LastRoutingClosingPosition { get; set; }
        
        public DateTime? LastTradedTime { get; set; }
        
        public bool CLosing { get; set; }
        
        #endregion
        
        #region Statistical Data

        public string ImbalanceSummary
        {
            get
            {
                return string.Format("{0} - Last Price:{1} ExpMovShort: {2} ExpMovLong:{3}", Pair.Symbol,
                    ExponentialMovingAverageLong.GetLastPrice()!=null?  ExponentialMovingAverageLong.GetLastPrice().Value.ToString("0.##"):"-",
                    ExponentialMovingAverageLong.Average.ToString("0.##"),
                    ExponentialMovingAverageShort.Average.ToString("0.##"));
            }

        }

        #endregion
        
        #region Public Methods

        public string TradeDirection()
        {
            if(LastRoutingOpeningPosition!=null)
            {
                return LastRoutingOpeningPosition.Side == Side.Buy ? "LONG" : "SHORT";
            }
            else
            {
                return "-";
            }
        }

        public bool IsFirstLeg()
        {
            return LastRoutingClosingPosition == null;
        }
        
        public bool IsSecondLeg()
        {
            return LastRoutingClosingPosition != null;
        }
        
        public Position CurrentPos()
        {
            return LastRoutingClosingPosition == null ? LastRoutingOpeningPosition : LastRoutingClosingPosition;
        
        }

        public bool CloseLongPosition()
        {
            if (LastRoutingOpeningPosition != null)
            {
                return LastRoutingOpeningPosition.Side == Side.Buy && CloseLong();
            }
            else
            {
                return false;
            }

        }
        
        public bool CloseShortPosition()
        {
            if (LastRoutingOpeningPosition != null)
            {
                return LastRoutingOpeningPosition.Side == Side.Sell && CloseShort();
            }
            else
            {
                return false;
            }

        }

        public bool IsOpenPosition()
        {

            if (LastRoutingOpeningPosition!=null && LastRoutingOpeningPosition.FilledPos() )
            {
                if (LastRoutingClosingPosition == null)
                    return true;
                else
                    return false;//Closing Pos still routing or closed
            }
            else
            {
                if (LastRoutingOpeningPosition!=null &&LastRoutingClosingPosition == null)
                    return true;
                else
                    return false;
                
            }
        }

        public bool CanOpenRoutingPosition()
        {
            if (LastRoutingOpeningPosition == null ||
                (LastRoutingClosingPosition != null && LastRoutingClosingPosition.FilledPos()))
            {

                return true;
            }
            else
            {
                return false;
            }

        }

        public bool EvalTradingConditions()
        {
            
            if ( (ExponentialMovingAverageLong.Length < ExponentialMovingAverageLong.Prices.Count)
                && (ExponentialMovingAverageShort.Length < ExponentialMovingAverageShort.Prices.Count)
                )
                return true;
            else
            {
                return false;
            }
            
        }

        public bool CloseLong()
        {
            if (!EvalTradingConditions())
                return false;
            
            return ExponentialMovingAverageShort.Average < ExponentialMovingAverageLong.Average;
        }

        public bool CloseShort()
        {
            if (!EvalTradingConditions())
                return false;
            
            return ExponentialMovingAverageLong.Average < ExponentialMovingAverageShort.Average;
        }

        public bool LongSignalTriggered()
        {
            if (!EvalTradingConditions())
                return false;
            
            return ExponentialMovingAverageShort.Average > ExponentialMovingAverageLong.Average;
        }
        
        public bool ShortSignalTriggered()
        {
            if (!EvalTradingConditions())
                return false;
            
            return ExponentialMovingAverageLong.Average > ExponentialMovingAverageShort.Average;
        }

        public void UpdatePrice(MarketData md)
        {
            ExponentialMovingAverageLong.UpdatePrice(md);
            ExponentialMovingAverageShort.UpdatePrice(md);
        }
        
        #endregion
    }
}