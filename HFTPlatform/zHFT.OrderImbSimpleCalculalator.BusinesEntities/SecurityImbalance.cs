using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.OrderImbSimpleCalculator.BusinesEntities;

namespace zHFT.OrderImbSimpleCalculator.BusinessEntities
{
    public class SecurityImbalance
    {
        #region Constructors

        public SecurityImbalance()
        {
            ImbalanceCounter = new ImbalanceCounter();

            Closing = false;
        }

        #endregion

        #region Public Attributes

        public Security Security { get; set; }
        
        public MarketData OpeningPrice { get; set; }

        public DateTime DateTime { get; set; }

        public ImbalanceCounter ImbalanceCounter { get; set; }
        
        public Position LastOpenedPosition { get; set; }
        
        public DateTime? LastTradedTime { get; set; }

        public bool Closing { get; set; }

        #endregion

        #region Config Data

        public int? DecimalRounding { get; set; }

        #endregion

        #region Statistical Data

        public string ImbalanceSummary
        {

            get
            {
                return string.Format("{0} - Imbalance Bid:{1} Imbalance Ask: {2}", Security.Symbol,
                    ImbalanceCounter.BidSizeImbalance.ToString("0.##"),
                    ImbalanceCounter.AskSizeImbalance.ToString("0.##"));
            }

        }

        #endregion

        #region Public Methods

        public void ProcessCounters()
        {
            ImbalanceCounter.ProcessCounters(Security, DecimalRounding);
        }

        public void ResetAll()
        {
            ImbalanceCounter.ResetOldBlocks();
        }

        public bool LongPositionThresholdTriggered(decimal positionOpeningImbalanceThreshold)
        {
            return ImbalanceCounter.LongPositionThresholdTriggered(positionOpeningImbalanceThreshold);
        }

        public bool ShortPositionThresholdTriggered(decimal positionOpeningImbalanceThreshold)
        {
            return ImbalanceCounter.ShortPositionThresholdTriggered(positionOpeningImbalanceThreshold);

        }

        public virtual void AppendMarketData(MarketData md)
        {
            Security.MarketData = md;

            if (OpeningPrice == null)
                OpeningPrice = md;

            //Sometimes the first MD might not have a Trade
            if (OpeningPrice != null && !OpeningPrice.Trade.HasValue && md != null && md.Trade.HasValue)
                OpeningPrice.Trade = md.Trade;
        }

        public bool ValidPacing(int maxMinWaitBtwConsecutivePos)
        {
            if (LastOpenedPosition != null && LastTradedTime.HasValue)
            {
                TimeSpan elapsed = DateTime.Now - LastTradedTime.Value;

                return elapsed.TotalMinutes > maxMinWaitBtwConsecutivePos;
            }
            else 
                return true;
        }

        public bool IsLongDay()
        {
            if (OpeningPrice != null && Security.MarketData != null)
            {
                return Security.MarketData.Trade > OpeningPrice.Trade;
            }
            else
            {
                return false;
            }
        }
        
        public bool IsShortDay()
        {
            if (OpeningPrice != null && Security.MarketData != null)
            {
                return Security.MarketData.Trade < OpeningPrice.Trade;
            }
            else
            {
                return false;
            }
        }

        #endregion
    }
}
