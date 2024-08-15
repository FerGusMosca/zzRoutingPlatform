using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{
    public class MonPosInnerIndicatorsOrchestationLogicDTO
    {

        #region Private Consts

        private static string _ORCH_LOGIC_ALL_IND = "ALL_IND";//AND--> All indicators must be met

        private static string _ORCH_LOGIC_FIRST_SIGNAL = "FIRST_SIGNAL";//OR --> Only one must be met

        private static string _ORCH_LOGIC_CUSTOM_QTY_SIGNALS = "CUSTOM_QTY_SIGNALS";//Certain amount of indicators

        #endregion

        #region Public Attributtes

        public string orchestationLogic { get; set; }

        public int? qtySignals { get; set; }

        #endregion

        #region Public Methods

        public void ValidateOrchestationLogic(string symbol)
        {

            if (orchestationLogic == null)
                return;//NO orchestation logic default = ALL_IND

            if (!IsAllIndicators() && !IsFirstSignal() && !IsCustomQtySignals())
                throw new Exception($"Invalid value for orchestation logic for symbol {symbol}: {orchestationLogic}");

            if (IsCustomQtySignals())
            {
                if (!qtySignals.HasValue)
                    throw new Exception($"Must provide qtySingals attribute when orchestation logic is CUSTOM_QTY_SIGNALS for symbol {symbol} ");

                if (qtySignals.Value < 0)
                    throw new Exception($"Invalid value of qtySingals when orchestation logic is CUSTOM_QTY_SIGNALS for symbol {symbol}: value {qtySignals.Value}");
            }

        }

        public bool IsAllIndicators()
        {
           
            return orchestationLogic == _ORCH_LOGIC_ALL_IND;
        
        }

        public bool IsFirstSignal() {

            return orchestationLogic == _ORCH_LOGIC_FIRST_SIGNAL;
        }


        public bool IsCustomQtySignals()
        {

            return orchestationLogic == _ORCH_LOGIC_CUSTOM_QTY_SIGNALS;
        }


        #endregion
    }
}
