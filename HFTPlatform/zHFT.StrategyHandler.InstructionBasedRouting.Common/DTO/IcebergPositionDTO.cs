using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.Enums;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;

namespace zHFT.StrategyHandler.InstructionBasedRouting.Common.DTO
{
    public class IcebergPositionDTO
    {
        #region Public Attributes

        public Instruction Instruction { get; set; }

        public List<Position> Positions { get; set; }

        #region Summary Ammount

        public double TotalAmmount { get; set; } //Monto total en moneda destino. Ej: ETH

        public double CumAmmount { get; set; }//Monto acumulado en moneda destino. Ej: ETH

        public double LeavesAmmount { get; set; }//Monto remanente en moneda destino. Ej: ETH

        public double CurrentStepAmmount { get; set; }//Monto step actual en moneda destino. Ej: ETH

        public double StepAmmount { get; set; } //Monto del step actual en moneda destino

        #endregion

        #region Quote Currency Ammount

        public double QuoteCurrencyTotalAmmount { get; set; } //Monto total en moneda quote. Ej: BTC

        public double QuoteCurrencyCumAmmount { get; set; }//Monto acumulado en moneda quote. Ej: BTC

        public double QuoteCurrencyLeavesAmmount { get; set; }//Monto remanente en moneda quote. Ej: BTC

        public double QuoteCurrencyCurrentStepAmmount { get; set; }//Monto step actual en moneda quote. Ej: BTC

        public double QuoteCurrencyStepAmmount { get; set; } //Monto del step actual en moneda quote. Ej: BTC

        #endregion

        public int CurrentStep { get; set; }

        public zHFT.Main.Common.Enums.PositionStatus PositionStatus { get; set; }

        public zHFT.Main.Common.Enums.Side Side { get; set; }


        #endregion

        #region Public Methods

        public double CalculateNextStepAmmountInSummaryCurrency()
        {
            if (LeavesAmmount > StepAmmount)
                return StepAmmount;
            else
                return TotalAmmount - CumAmmount;
        }

        public double CalculateNextStepAmmountInQuoteCurrency()
        {
            if (LeavesAmmount > StepAmmount)
                return StepAmmount;
            else
                return TotalAmmount - CumAmmount;
        }



        #endregion
    }
}
