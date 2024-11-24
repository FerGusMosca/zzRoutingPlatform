using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{

    public class MonPosTrutleIndicatorConfigDTO: TurtleIndicatorBaseConfigDTO
    {
        #region Public Attributes

        public string candleReferencePrice { get; set; }    


        public double stopLossForOpenPositionPct { get; set; }

        public bool? closeOnInnerIndicators { get;set; }


        public MonPosInnerIndicatorsOrchestationLogicDTO innerIndicatorsOrchestationLogic {  get; set; }

        #endregion


        #region Public Methods

        public bool DoCloseOnInnnerIndicators()
        {
            if (closeOnInnerIndicators.HasValue)
            {
                return closeOnInnerIndicators.Value;
            }
            else
                return true;
        
        }

        #endregion
    }
}
