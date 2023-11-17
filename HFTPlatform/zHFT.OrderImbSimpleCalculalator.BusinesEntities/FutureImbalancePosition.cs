using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.OrderImbSimpleCalculator.BusinessEntities
{
    public class FutureImbalancePosition : BasePortfImbalancePosition
    {
        #region Public Methods

        public double ContractSize { get; set; }

        public double Margin { get; set; }

        public override double Qty
        {
            get
            {
                if (OpeningPosition.QuantityType == QuantityType.CONTRACTS || OpeningPosition.QuantityType == QuantityType.OTHER)
                    return OpeningPosition.CumQty;
                else
                    throw new Exception(string.Format("Future Position: Qty not available for Qty type {0}", OpeningPosition.QuantityType));
            }
        }

        public override double TotalFee
        {
            get
            {

                if (FeeTypePerTrade == zHFT.OrderImbSimpleCalculator.Common.Enums.FeeTypePerTrade.OrderRouter.ToString())
                {
                    return base.TotalFee;
                }
                else if (FeeTypePerTrade == zHFT.OrderImbSimpleCalculator.Common.Enums.FeeTypePerTrade.Nominal.ToString())
                {
                    return base.TotalFee;
                }
                else if (FeeTypePerTrade == zHFT.OrderImbSimpleCalculator.Common.Enums.FeeTypePerTrade.Percentage.ToString())
                {
                    double fee = FeeValuePerTrade * InitialCap;
                    return fee;
                }
                else
                    return 0;
            }
        }

        public override double InitialCap
        {
            get
            {
                if (!OpeningPosition.AvgPx.HasValue)
                    throw new Exception(string.Format("Unknown AvgPx for position on Security {0}", OpeningPosition.Security.Symbol));

                return OpeningPosition.CumQty * ContractSize * Margin ;
            }
        }

        public override double? FinalCap
        {
            get
            {
                if (ClosingPosition != null)
                {
                    if (!ClosingPosition.AvgPx.HasValue)
                        throw new Exception(string.Format("Unknown AvgPx for position on Security {0}", OpeningPosition.Security.Symbol));


                    double priceDiff = 0;
                    if (TradeDirection == _LONG)
                        priceDiff = ClosingPosition.AvgPx.Value - OpeningPosition.AvgPx.Value;
                    else
                        priceDiff = OpeningPosition.AvgPx.Value - ClosingPosition.AvgPx.Value;

                    return (priceDiff / OpeningPosition.AvgPx) * ContractSize * OpeningPosition.CumQty;
                }
                else
                    return InitialCap;
            }
        }

        public override double? Profit
        {

            get
            {
                if (FinalCap.HasValue && InitialCap != 0)
                    return (((FinalCap - TotalFee) / InitialCap) - 1);
                else
                    return null;
            }

        }

        public override double? NominalProfit
        {

            get
            {
                if (TradeDirection == _LONG)
                {
                    if (FinalCap.HasValue && InitialCap != 0)
                        return (FinalCap - TotalFee);
                    else
                        return null;
                }
                else if (TradeDirection == _SHORT)
                {
                    if (FinalCap.HasValue && FinalCap.HasValue && FinalCap.Value != 0)
                        return (FinalCap - TotalFee);
                    else
                        return null;
                }
                else
                    return null;
            }

        }


        #endregion
    }
}
