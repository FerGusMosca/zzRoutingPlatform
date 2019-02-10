using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.Enums;

namespace zHFT.OrderImbSimpleCalculator.BusinessEntities
{
    public class ImbalancePosition
    {
        #region Public Attribute

        public DateTime OpeningDate { get; set; }

        public DateTime? ClosingDate { get; set; }

        public SecurityImbalance OpeningImbalance { get; set; }

        public SecurityImbalance ClosingImbalance { get; set; }

        public Position OpeningPosition { get; set; }

        public Position ClosingPosition { get; set; }

        public string FeeTypePerTrade { get; set; }

        public double FeeValuePerTrade { get; set; }

        #region Calculated Attrs

        public double Qty { 
            get 
            {
                if (OpeningPosition.QuantityType == QuantityType.SHARES)
                    return OpeningPosition.CumQty;
                else
                    throw new Exception (string.Format("Qty not available for Qty type {0}",OpeningPosition.QuantityType));
            } 
        }

        public string TradeDirection
        {
            get { return OpeningPosition.Side == Side.Buy ? "LONG" : "SHORT"; }
        }

        public double OpeningPrice
        {
            get {

                if (OpeningPosition.AvgPx.HasValue)
                    return OpeningPosition.AvgPx.Value;
                else
                    throw new Exception(string.Format("Opening price not available for trade on symbol {0}", OpeningPosition.Security.Symbol));
            
            }
        }

        public double ClosingPrice
        {
            get
            {

                if (ClosingPosition != null && ClosingPosition.AvgPx.HasValue)
                    return ClosingPosition.AvgPx.Value;
                else
                    throw new Exception(string.Format("Closing price not available for trade on symbol {0}", OpeningPosition.Security.Symbol));

            }
        }

        public double TotalFee
        {
            get
            {

                if (FeeTypePerTrade == zHFT.OrderImbSimpleCalculator.Common.Enums.FeeTypePerTrade.OrderRouter.ToString())
                {
                    double fee = 0;

                    OpeningPosition.ExecutionReports.ForEach(x => fee += x.Commission.HasValue ? x.Commission.Value : 0);

                    if (ClosingPosition != null)
                        ClosingPosition.ExecutionReports.ForEach(x => fee += x.Commission.HasValue ? x.Commission.Value : 0);

                    return fee;
                }
                else if (FeeTypePerTrade == zHFT.OrderImbSimpleCalculator.Common.Enums.FeeTypePerTrade.Nominal.ToString())
                {
                    double fee = FeeValuePerTrade;

                    if (ClosingPosition != null)
                        fee += FeeValuePerTrade;
                    return fee;
                }
                else if (FeeTypePerTrade == zHFT.OrderImbSimpleCalculator.Common.Enums.FeeTypePerTrade.Percentage.ToString())
                {
                    double fee = FeeValuePerTrade * InitialCap;

                    if (ClosingPosition != null)
                        fee += FeeValuePerTrade * FinalCap;

                    return fee;
                }
                else
                    return 0;
            }
        }

        public double InitialCap
        {
            get
            {

                if (OpeningPosition.QuantityType == QuantityType.SHARES)
                {
                    if (!OpeningPosition.AvgPx.HasValue)
                        throw new Exception(string.Format("Unknown AvgPx for position on Security {0}", OpeningPosition.Security.Symbol));

                    return OpeningPosition.CumQty * OpeningPosition.AvgPx.Value;
                }
                else
                    throw new Exception(string.Format("Qty not available for Qty type {0}", OpeningPosition.QuantityType));

            }
        }

        public double FinalCap
        {
            get
            {
                if (ClosingPosition != null)
                {
                    if (ClosingPosition.QuantityType == QuantityType.SHARES)
                    {
                        if (!ClosingPosition.AvgPx.HasValue)
                            throw new Exception(string.Format("Unknown AvgPx for position on Security {0}", OpeningPosition.Security.Symbol));

                        return (OpeningPosition.CumQty * ClosingPosition.AvgPx.Value) - TotalFee;
                    }
                    else
                        throw new Exception(string.Format("Qty not available for Qty type {0}", OpeningPosition.QuantityType));
                }
                else
                    return InitialCap;
            }
        }

        public double Profit
        {

            get 
            {
                return ((FinalCap - TotalFee) / InitialCap) - 1;
            }
        
        }

        public string OpeningImbalanceSummary
        {
            get {

                if (OpeningImbalance != null)
                    return OpeningImbalance.ImbalanceSummary;
                else
                    throw new Exception(string.Format("Missing Opening Imbalance for security {0}", OpeningImbalance.Security.Symbol));
            
            
            }
        }


        public string ClosingImbalanceSummary
        {
            get
            {
                if (ClosingImbalance != null)
                    return ClosingImbalance.ImbalanceSummary;
                else
                    throw new Exception(string.Format("Missing Closing Imbalance for security {0}", ClosingImbalance.Security.Symbol));
            }
        }


        #endregion

        #endregion

        #region Public Methods

        public Position CurrentPos()
        {
            return ClosingPosition == null ? OpeningPosition : ClosingPosition;
        
        }

        public bool IsFirstLeg()
        {
            return ClosingPosition == null;
        }

        public bool EvalStopLossHit(SecurityImbalance secImb)
        {
            if (IsFirstLeg())
            {
                //TODO: Eval que pasa si estoy cerrando una posición parcialmente abierta
                if (OpeningPosition.Side == Side.Buy && (OpeningPosition.PosStatus == PositionStatus.PartiallyFilled || OpeningPosition.PosStatus == PositionStatus.Filled))
                {
                    return OpeningPosition.AvgPx.HasValue && secImb.Security.MarketData.Trade.HasValue ?
                         ((OpeningPosition.AvgPx.Value * (1 - OpeningPosition.StopLossPct)) > secImb.Security.MarketData.Trade.Value) : false;
                }


                if (OpeningPosition.Side == Side.Sell && (OpeningPosition.PosStatus == PositionStatus.PartiallyFilled || OpeningPosition.PosStatus == PositionStatus.Filled))
                {
                    return OpeningPosition.AvgPx.HasValue && secImb.Security.MarketData.Trade.HasValue ?
                           ((OpeningPosition.AvgPx.Value * (1 - OpeningPosition.StopLossPct)) < secImb.Security.MarketData.Trade.Value) : false;
                }
                return false;
            }

            return false;
        }

        #endregion
    }
}
