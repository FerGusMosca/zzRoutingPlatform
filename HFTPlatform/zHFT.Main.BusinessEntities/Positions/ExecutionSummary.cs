using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.BusinessEntities.Positions
{
    public class ExecutionSummary
    {
        #region Public Attributes

        public long Id { get; set; }

        public DateTime Date { get; set; }

        public Position Position { get; set; }

        public string Symbol { get; set; }

        public double? AvgPx { get; set; }

        public double CumQty { get; set; }

        public double LeavesQty { get; set; }

        public double? Commission { get; set; }

        public string Text { get; set; }

        public string Console { get; set; }

        public int? AccountNumber{ get; set; }

        #endregion

        #region Public Methods

        public double GetCashExecution()
        {
            double acum = 0;

            if (Position.PositionCleared)
            {
                if (AvgPx.HasValue)
                {
                    acum = CumQty * AvgPx.Value;

                    if (Commission != null)
                        acum += Commission.Value;
                    
                    return acum;
                }
                else
                    throw new Exception(string.Format("Could not process avg px for a closed position for symbol {0}", Position.Symbol));
            }
            else
                return 0;
        }

        public bool IsFilledPosition()
        {
            return LeavesQty <= 0 && CumQty > 0
                   && (Position == null || Position.PosStatus == PositionStatus.Filled);
        }

        #endregion
    }
}
