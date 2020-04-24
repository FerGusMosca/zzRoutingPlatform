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

        #region Public Static COnsts

        public static string _LONG = "LONG";

        public static string _SHORT = "SHORT";

        #endregion

        #region Public Attribute

        public string StrategyName { get; set; }

        public DateTime OpeningDate { get; set; }

        public DateTime? ClosingDate { get; set; }

        public SecurityImbalance OpeningImbalance { get; set; }

        public SecurityImbalance ClosingImbalance { get; set; }

        public Position OpeningPosition { get; set; }

        public Position ClosingPosition { get; set; }

        public string FeeTypePerTrade { get; set; }

        public double FeeValuePerTrade { get; set; }

        public double? LastPrice { get; set; }

        #region Calculated Attrs

        public virtual double Qty { 
            get 
            {
                if (   OpeningPosition.QuantityType == QuantityType.SHARES 
                    || OpeningPosition.QuantityType == QuantityType.CURRENCY )
                    return OpeningPosition.CumQty;
                else
                    throw new Exception (string.Format("Qty not available for Qty type {0}",OpeningPosition.QuantityType));
            } 
        }

        public string TradeDirection
        {
            get { return OpeningPosition.Side == Side.Buy ? _LONG : _SHORT; }
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

        public double? ClosingPrice
        {
            get
            {

                if (ClosingPosition != null && ClosingPosition.AvgPx.HasValue)
                    return ClosingPosition.AvgPx.Value;
                else
                    return null;

            }
        }

        public virtual double TotalFee
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

                    if (ClosingPosition != null && ClosingPosition.AvgPx.HasValue)
                        fee += FeeValuePerTrade * ClosingPosition.CumQty * ClosingPosition.AvgPx.Value ;

                    return fee;
                }
                else
                    return 0;
            }
        }

        public virtual double InitialCap
        {
            get
            {
                if (!OpeningPosition.AvgPx.HasValue)
                    throw new Exception(string.Format("Unknown AvgPx for position on Security {0}", OpeningPosition.Security.Symbol));

                return OpeningPosition.CumQty * OpeningPosition.AvgPx.Value;
            }
        }

        public virtual double? FinalCap
        {
            get
            {
                if (ClosingPosition != null)
                {
                    if (ClosingPosition.QuantityType == QuantityType.SHARES)
                    {
                        if (!ClosingPosition.AvgPx.HasValue)
                            throw new Exception(string.Format("Unknown AvgPx for position on Security {0}", OpeningPosition.Security.Symbol));

                        return (OpeningPosition.CumQty * ClosingPosition.AvgPx.Value) ;
                    }
                    else
                        return null;
                }
                else
                    return InitialCap;
            }
        }

        public virtual double? Profit
        {

            get 
            {
                if (TradeDirection == _LONG)
                {
                    if (FinalCap.HasValue && InitialCap != 0)
                        return (((FinalCap - TotalFee) / InitialCap) - 1) ;
                    else
                        return null;
                }
                else if (TradeDirection == _SHORT)
                {
                    if (FinalCap.HasValue && FinalCap.HasValue && FinalCap.Value != 0)
                        return (((InitialCap) / (FinalCap + TotalFee)) - 1) ;
                    else
                        return null;
                }
                else
                    return null;
            }
        
        }

        public virtual double? NominalProfit
        {

            get
            {
                if (TradeDirection == _LONG)
                {
                    if (FinalCap.HasValue && InitialCap != 0)
                        return (FinalCap - TotalFee - InitialCap);
                    else
                        return null;
                }
                else if (TradeDirection == _SHORT)
                {
                    if (FinalCap.HasValue && FinalCap.HasValue && FinalCap.Value != 0)
                        return (InitialCap - TotalFee - FinalCap) ;
                    else
                        return null;
                }
                else
                    return null;
            }

        }

        public string OpeningImbalanceSummary
        {
            get {

                if (OpeningImbalance != null)
                    return OpeningImbalance.ImbalanceSummary;
                else
                    return string.Format("Missing Opening Imbalance for security {0}. Recovery?", OpeningPosition.Security.Symbol);
            
            }
        }

        public string ClosingImbalanceSummary
        {
            get
            {
                if (ClosingImbalance != null)
                    return ClosingImbalance.ImbalanceSummary;
                else
                    return "";
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
            if (secImb.Closing)
                return false;

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
                           ((OpeningPosition.AvgPx.Value * (1 + OpeningPosition.StopLossPct)) < secImb.Security.MarketData.Trade.Value) : false;
                }
                return false;
            }

            return false;
        }


        public bool EvalClosingShortPosition(SecurityImbalance secImb,decimal positionOpeningImbalanceMaxThreshold)
        {
            return (TradeDirection == ImbalancePosition._SHORT && !secImb.Closing
                   && secImb.ImbalanceCounter.BidSizeImbalance < positionOpeningImbalanceMaxThreshold
                   && (OpeningPosition.PosStatus == PositionStatus.Filled || OpeningPosition.PosStatus == PositionStatus.PartiallyFilled));
        }

       

        public bool EvalClosingLongPosition(SecurityImbalance secImb, decimal positionOpeningImbalanceMaxThreshold)
        {
            return (TradeDirection == ImbalancePosition._LONG && !secImb.Closing
                   && secImb.ImbalanceCounter.AskSizeImbalance < positionOpeningImbalanceMaxThreshold
                   && (OpeningPosition.PosStatus == PositionStatus.Filled || OpeningPosition.PosStatus == PositionStatus.PartiallyFilled));
        }

        public bool EvalAbortingNewLongPosition(SecurityImbalance secImb, decimal PositionOpeningImbalanceThreshold)
        {
            return (TradeDirection == ImbalancePosition._LONG
                   && secImb.ImbalanceCounter.AskSizeImbalance < PositionOpeningImbalanceThreshold
                   && OpeningPosition.PositionRouting());
        }

        public bool EvalAbortingNewShortPosition(SecurityImbalance secImb, decimal PositionOpeningImbalanceThreshold)
        {
            return (TradeDirection == ImbalancePosition._SHORT
                   && secImb.ImbalanceCounter.BidSizeImbalance < PositionOpeningImbalanceThreshold
                    && OpeningPosition.PositionRouting());
        }

        public bool EvalAbortingClosingLongPosition(SecurityImbalance secImb, decimal positionOpeningImbalanceMaxThreshold)
        {
            return (TradeDirection == ImbalancePosition._LONG
                   && ClosingPosition!=null
                   && secImb.ImbalanceCounter.AskSizeImbalance > positionOpeningImbalanceMaxThreshold
                   && ClosingPosition.PositionRouting());
        }

        public bool EvalAbortingClosingShortPosition(SecurityImbalance secImb, decimal positionOpeningImbalanceMaxThreshold)
        {
            return (TradeDirection == ImbalancePosition._SHORT
                   && ClosingPosition != null
                   && secImb.ImbalanceCounter.BidSizeImbalance > positionOpeningImbalanceMaxThreshold
                   && ClosingPosition.PositionRouting());
        }


      

        #endregion
    }
}
