using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
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

        public DateTime DateTime { get; set; }

        public ImbalanceCounter ImbalanceCounter { get; set; }

        public bool Closing { get; set; }

        #endregion

        #region Config Data

        public int? DecimalRounding { get; set; }

        #endregion

        #region Statistical Data

        public string ImbalanceSummary {

            get {

                return string.Format("{0} - Imbalance Bid:{1} Imbalance Ask {2}", Security.Symbol, ImbalanceCounter.BidSizeImbalance.ToString("0.##"), ImbalanceCounter.AskSizeImbalance.ToString("0.##"));
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
            return ImbalanceCounter.LongPositionThresholdTriggered(positionOpeningImbalanceThreshold) ;
        }

        public bool ShortPositionThresholdTriggered(decimal positionOpeningImbalanceThreshold)
        {
            return ImbalanceCounter.ShortPositionThresholdTriggered(positionOpeningImbalanceThreshold);

        }

        #endregion
    }
}
