using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.DTO;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace zHFT.StrategyHandler.InstructionBasedRouting
{
    public class CryptosInstructionBasedRouter : InstructionBasedRouter
    {

        #region Protected Methods

        protected override void DoClear()
        {
            PositionInstructions.Clear();
            ExecutionSummaries.Clear();
            Positions.Clear();
        }

        protected override bool EvalMarketData(ExecutionSummary summary)
        {
            return true;
        }


        protected override void ProcessUnwindPosition(Instruction instr, AccountPosition portfPos)
        {

            DoLog(string.Format("{0}: Unwinding position for symbol {1}", IBRConfiguration.Name, instr.Symbol), Constants.MessageType.Information);

            Position pos = new Position()
            {
                Security = new Security()
                {
                    Symbol = instr.Symbol,
                    MarketData = null,
                    Currency = instr.Account.Currency,
                    SecType = instr.SecurityType
                },
                Side = zHFT.Main.Common.Enums.Side.Sell,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew
            };

            if (instr.Ammount.HasValue)
            {
                pos.Qty = Convert.ToDouble(instr.Ammount);
                pos.QuantityType = QuantityType.SHARES;
            }
            else
            {
                MarkAsUnwindRejected(ref instr, string.Format("{0}: Discarding unwind position because it was not specified a number of shares. Symbol = {1}", IBRConfiguration.Name, instr.Symbol));
                return;
            }

            pos.LoadPosId(NextPosId);
            NextPosId++;

            ExecutionSummary summary = new ExecutionSummary()
            {
                Date = DateTime.Now,
                Position = pos,
                Symbol = pos.Security.Symbol,
                AvgPx = null,
                CumQty = 0
            };

            ExecutionSummaries.Add(pos.Security.Symbol, summary);
            PositionInstructions.Add(pos.Security.Symbol, instr);
            
        }

        #endregion
    }
}
