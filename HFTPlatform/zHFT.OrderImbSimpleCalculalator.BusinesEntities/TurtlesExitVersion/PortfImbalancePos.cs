using System;
using System.Collections.Generic;
using System.Linq;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;

namespace zHFT.OrderImbSimpleCalculator.BusinessEntities
{
    public class PortfImbalancePos:BasePortfImbalancePosition
    {
        
        #region Public Static Consts

        public static string _CANLDE_REF_PRICE_TRADE = "TRADE";
        public static string _CANLDE_REF_PRICE_CLOSE = "CLOSE";
        
        #endregion
        
        #region Public Overriden Methods
        
        public override string ClosingSummary(BaseMonSecurityImbalance monImb)
        {
            MonSecurityImbalance tSecImb = (MonSecurityImbalance) monImb;

            string strOpeningPrice = tSecImb.OpeningPrice != null && tSecImb.OpeningPrice.Trade.HasValue
                    ? tSecImb.OpeningPrice.Trade.Value.ToString("#.##")
                    : "<no trade>";

            string strLastTrade = tSecImb.Security != null && tSecImb.Security.MarketData != null &&
                tSecImb.Security.MarketData.Trade.HasValue
                    ? tSecImb.Security.MarketData.Trade.Value.ToString("#.##")
                    : "<no market data>";

            string exitCond = "";
            string exitValue = "";
            if (monImb.CustomImbalanceConfig.CloseTurtles)
            {
                exitCond = "TURTLES";
            }
            else if (monImb.CustomImbalanceConfig.CloseMMov)
            {
                exitCond = "MMOV";
                exitValue = monImb.CalculateSimpleMovAvg(monImb.CustomImbalanceConfig.CloseWindow.Value).ToString("#.##");

            }
            else if (monImb.CustomImbalanceConfig.CloseOnImbalance)
            {
                exitCond = "IMBALANCE";
                exitValue = $"Bid Imbalance={monImb.ImbalanceCounter.BidSizeImbalance} Ask Imbalance={monImb.ImbalanceCounter.AskSizeImbalance}";
            }
            else 
            {
                exitCond = "???";
            }

            return $"Symbol={tSecImb.Security.Symbol} IsLongDay={tSecImb.IsLongDay()} " +
                    $"OpeningTrade={strOpeningPrice} LastTrade={strLastTrade} " +
                    $"ExitCond={exitCond} ExitValue={exitValue}";
          
        }

        public override bool EvalClosingShortPosition(BaseMonSecurityImbalance monImb)
        {

            if (monImb is MonSecurityImbalance)
            {
                
                MonSecurityImbalance tSecImb = (MonSecurityImbalance) monImb;
                bool closeSignal = false;

                if (monImb.CustomImbalanceConfig.CloseTurtles)
                    closeSignal = monImb.IsHighest(tSecImb.CustomImbalanceConfig.CloseWindow.Value);
                else if (monImb.CustomImbalanceConfig.CloseMMov)
                    closeSignal = monImb.IsHigherThanMMov(tSecImb.CustomImbalanceConfig.CloseWindow.Value, false);
                else if (monImb.CustomImbalanceConfig.CloseOnImbalance)
                    closeSignal = base.EvalClosingShortPosition(monImb);
                else
                    throw new Exception($"Not closing config setup for security {monImb.Security.Symbol}");

                return (TradeDirection == BasePortfImbalancePosition._SHORT 
                        && !monImb.Closing
                       && closeSignal
                       && (OpeningPosition.PosStatus == PositionStatus.Filled || OpeningPosition.PosStatus == PositionStatus.PartiallyFilled));

            }
            else
            {
                throw new Exception(string.Format(
                    "Critical ERROR @ImbalancePositionTurtlesExit.EvalClosingShortPosition: secImb parameter must be of SecurityImbalanceTurtlesExit type"));
            }
        }
        
        public override bool EvalClosingLongPosition(BaseMonSecurityImbalance monImb)
        {

            if (monImb is MonSecurityImbalance)
            {
                MonSecurityImbalance tSecImb = (MonSecurityImbalance) monImb;
                
                bool closeSignal = false;

                if (monImb.CustomImbalanceConfig.CloseTurtles)
                    closeSignal = monImb.IsLowest(tSecImb.CustomImbalanceConfig.CloseWindow.Value);
                else if (monImb.CustomImbalanceConfig.CloseMMov)
                    closeSignal = !monImb.IsHigherThanMMov(tSecImb.CustomImbalanceConfig.CloseWindow.Value, false);
                else if (monImb.CustomImbalanceConfig.CloseOnImbalance)
                    closeSignal = base.EvalClosingLongPosition(monImb);
                else
                    throw new Exception($"Not closing config setup for security {monImb.Security.Symbol}");

                bool closeLong= (TradeDirection == BasePortfImbalancePosition._LONG
                                && !monImb.Closing
                                && closeSignal
                                && (OpeningPosition.PosStatus == PositionStatus.Filled ||
                                    OpeningPosition.PosStatus == PositionStatus.PartiallyFilled));

                return closeLong;
            }
            else
            {
                throw new Exception(string.Format(
                    "Critical ERROR @ImbalancePositionTurtlesExit.EvalClosingLongPosition: secImb parameter must be of SecurityImbalanceTurtlesExit type"));
            }
        }
        
        public override bool EvalAbortingClosingLongPosition(BaseMonSecurityImbalance secImb)
        {
            return false; //Turtles dont abort closing /opening position
        }
        
        public override bool EvalAbortingClosingShortPosition(BaseMonSecurityImbalance secImb)
        {
            return false;//Turtles dont abort closing /opening position
        }
        
        public override bool EvalAbortingNewLongPosition(BaseMonSecurityImbalance secImb)
        {
            return false;//Turtles dont abort closing /opening position
        }

        public override bool EvalAbortingNewShortPosition(BaseMonSecurityImbalance secImb)
        {
            return false;//Turtles dont abort closing /opening position
        }
        
        #endregion
        
    }
}