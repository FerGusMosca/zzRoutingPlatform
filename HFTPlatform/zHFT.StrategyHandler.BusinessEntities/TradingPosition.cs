using System;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.Enums;

namespace zHFT.StrategyHandler.BusinessEntities
{
    public abstract class TradingPosition
    {
        #region Public Static COnsts

        public static string _LONG = "LONG";

        public static string _SHORT = "SHORT";

        #endregion

        #region Public Attributes

        public string StrategyName { get; set; }

        public Position OpeningPosition { get; set; }

        public Position ClosingPosition { get; set; }

        public DateTime OpeningDate { get; set; }

        public DateTime? ClosingDate { get; set; }

        public bool Closing { get; set; }

        public string TradeDirection
        {
            get { return OpeningPosition.Side == Side.Buy ? _LONG : _SHORT; }
        }

        public virtual double Qty {
            get
            {
                if (OpeningPosition.QuantityType == QuantityType.SHARES
                       || OpeningPosition.QuantityType == QuantityType.CURRENCY)
                    return OpeningPosition.CumQty;
                else
                    throw new Exception(string.Format("Qty not available for Qty type {0}", OpeningPosition.QuantityType));
            }
        }

        public string FeeTypePerTrade { get; set; }

        public double FeeValuePerTrade { get; set; }

        public MonitoringPosition OpeningPortfolioPosition { get; set; }

        public double? LastPrice { get; set; }

        #endregion

        #region Public Methods

        public Position CurrentPos()
        {
            return ClosingPosition == null ? OpeningPosition : ClosingPosition;

        }

        public double OpenCumQty()
        {
            if (OpeningPosition != null)
                return OpeningPosition.CumQty;
            else
                return 0;
        }

        public double CloseCumQty()
        {
            if (ClosingPosition != null)
                return ClosingPosition.CumQty;
            else
                return 0;
        }

        public bool IsFirstLeg()
        {
            return ClosingPosition == null;
        }

        public double? OpeningPrice
        {
            get {

                if (OpeningPosition.AvgPx.HasValue)
                    return OpeningPosition.AvgPx.Value;
                else
                    return null;
                //throw new Exception(string.Format("Opening price not available for trade on symbol {0}", OpeningPosition.Security.Symbol));

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

                if (FeeTypePerTrade == zHFT.StrategyHandler.Common.enums.FeeTypePerTrade.OrderRouter.ToString())
                {
                    double fee = 0;

                    OpeningPosition.ExecutionReports.ForEach(x =>
                        fee += x.Commission.HasValue ? x.Commission.Value : 0);

                    if (ClosingPosition != null)
                        ClosingPosition.ExecutionReports.ForEach(x =>
                            fee += x.Commission.HasValue ? x.Commission.Value : 0);

                    return fee;
                }
                else if (FeeTypePerTrade == zHFT.StrategyHandler.Common.enums.FeeTypePerTrade.Nominal.ToString())
                {
                    double fee = FeeValuePerTrade;

                    if (ClosingPosition != null)
                        fee += FeeValuePerTrade;
                    return fee;
                }
                else if (FeeTypePerTrade == zHFT.StrategyHandler.Common.enums.FeeTypePerTrade.Percentage.ToString())
                {
                    double fee = FeeValuePerTrade * InitialCap;

                    if (ClosingPosition != null && ClosingPosition.AvgPx.HasValue)
                        fee += FeeValuePerTrade * ClosingPosition.CumQty * ClosingPosition.AvgPx.Value;

                    return fee;
                }
                else
                    return 0;
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
                        return (InitialCap - TotalFee - FinalCap);
                    else
                        return null;
                }
                else
                    return null;
            }

        }

        public virtual double InitialCap
        {
            get
            {
                if (!OpeningPosition.AvgPx.HasValue)
                    return 0;
                //throw new Exception(string.Format("Unknown AvgPx for position on Security {0}", OpeningPosition.Security.Symbol));

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
                            return 0;
                        //throw new Exception(string.Format("Unknown AvgPx for position on Security {0}", OpeningPosition.Security.Symbol));

                        return (ClosingPosition.CumQty * ClosingPosition.AvgPx.Value);
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
                        return (((FinalCap - TotalFee) / InitialCap) - 1);
                    else
                        return null;
                }
                else if (TradeDirection == _SHORT)
                {
                    if (FinalCap.HasValue && FinalCap.HasValue && FinalCap.Value != 0)
                        return (((InitialCap) / (FinalCap + TotalFee)) - 1);
                    else
                        return null;
                }
                else
                    return null;
            }

        }

        #endregion

        #region Public Methods

        public bool IsLongDirection()
        {
            return TradeDirection == _LONG;
        }

        public bool IsShortDirection()
        {
            return TradeDirection == _SHORT;
        }

        public bool IsInFactClosedPortfPos()
        {
            if (OpeningPosition != null && ClosingPosition != null)
            {

                if (FinalCap.HasValue)
                {
                    double pct = Math.Abs(Convert.ToDouble(InitialCap / FinalCap.Value) - 1);

                    if (pct < 0.05)
                        return true;//Less that 5% difference btw buy and sell
                    else
                        return false;
                }
                else
                    return false;

            }
            else 
                return false;
        
        }




        #endregion
        
        #region Public Abstract Methods

        public abstract void DoCloseTradingPosition(TradingPosition trdPos);

        #endregion
    }
}