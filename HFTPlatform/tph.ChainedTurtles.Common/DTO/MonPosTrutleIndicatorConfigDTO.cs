using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{
    public class MonPosTrutleIndicatorConfigDTO
    {
        #region Public Attributes

        public string marketStartTime { get; set; }

        public string marketEndTime { get; set; }

        public string closingTime { get; set; } 

        public string candleReferencePrice { get; set; }    

        public int historicalPricesPeriod { get; set; }


        public double stopLossForOpenPositionPct { get; set; }

        public MonPosInnerIndicatorsOrchestationLogicDTO innerIndicatorsOrchestationLogic {  get; set; }

        #endregion
    }
}
