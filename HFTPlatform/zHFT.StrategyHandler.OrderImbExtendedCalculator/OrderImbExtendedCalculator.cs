using System;
using System.Collections.Generic;
using System.Threading;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.OrderImbSimpleCalculator.BusinessEntities;
using zHFT.OrderImbSimpleCalculator.Common.Configuration;
using zHFT.OrderImbSimpleCalculator.Common.Util;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.OrderImbSimpleCalculator;

namespace zHFT.StrategyHandler.OrderImbExtendedCalculator
{
    public class OrderImbExtendedCalculator:OrderImbalanceCalculator
    {
        #region Protected Attributes
        
        
        #endregion
        
        #region Protected Overriden Methods
        
        protected override BasePortfImbalancePosition LoadNewRegularPos(BaseMonSecurityImbalance secImb, Side side)
        {

            Position pos = new Position()
            {

                Security = secImb.Security,
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = Configuration.PositionSizeInCash,
                QuantityType = QuantityType.CURRENCY,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                StopLossPct = Convert.ToDouble(Configuration.StopLossForOpenPositionPct),
                AccountId = Configuration.Account,
            };

            pos.LoadPosId(NextPosId);
            NextPosId++;

            return new PortfImbalancePos()
            {
                StrategyName = Configuration.Name,
                OpeningDate = DateTime.Now,
                OpeningPosition = pos,
                OpeningImbalance = secImb,
                FeeTypePerTrade = Configuration.FeeTypePerTrade,
                FeeValuePerTrade = Configuration.FeeValuePerTrade
            };
        
        }

        public override void DoLoadConfig(string configFile, List<string> noValFields)
        {
            List<string> noValueFields = new List<string>();
            Config = new zHFT.OrderImbSimpleCalculator.Common.Configuration.ExtendedConfiguration().GetConfiguration<zHFT.OrderImbSimpleCalculator.Common.Configuration.ExtendedConfiguration>(configFile, noValueFields);
        }

        protected override void EvalClosingPosition(BaseMonSecurityImbalance monImbPos)
        {
            PortfImbalancePos portfImbPos = (PortfImbalancePos) PortfolioPositions[monImbPos.Security.Symbol];

            if (portfImbPos.EvalClosingShortPosition(monImbPos))
            {
                RunClose(portfImbPos.OpeningPosition, monImbPos, portfImbPos);
                DoLog(string.Format("Closing {0} Position on market w/Turtles. Symbol {1} Qty={2} Imbalance={3} PosId={4} ClosingSummary={5}", 
                        portfImbPos.TradeDirection, portfImbPos.OpeningPosition.Security.Symbol, portfImbPos.Qty,
                        monImbPos.ImbalanceSummary,
                        portfImbPos.ClosingPosition!=null? portfImbPos.ClosingPosition.PosId:"-",
                        portfImbPos.ClosingSummary(monImbPos)), 
                    Constants.MessageType.Information);
            }
            else if (portfImbPos.EvalClosingLongPosition(monImbPos))
            {
                RunClose(portfImbPos.OpeningPosition, monImbPos, portfImbPos);
                DoLog(string.Format("Closing {0} Position on market w/Turtles. Symbol {1} Qty={2}  Imbalance={3} PosId={4} ClosingSummary={5}",
                        portfImbPos.TradeDirection, portfImbPos.OpeningPosition.Security.Symbol, portfImbPos.Qty,
                        monImbPos.ImbalanceSummary,
                        portfImbPos.ClosingPosition != null ? portfImbPos.ClosingPosition.PosId : null,
                        portfImbPos.ClosingSummary(monImbPos)),
                    Constants.MessageType.Information);
            }
         
        }
        
        protected  override void LoadMonitorsAndRequestMarketData()
        {
            Thread.Sleep(5000);
            foreach (string symbol in Configuration.StocksToMonitor)
            {
                if (!SecurityImbalancesToMonitor.ContainsKey(symbol))
                {
                    Security sec = new Security()
                    {
                        Symbol = symbol,
                        SecType = Security.GetSecurityType(Configuration.SecurityTypes),
                        MarketData = new MarketData() { SettlType = SettlType.Tplus2 },
                        Currency = Configuration.Currency,
                        Exchange = Configuration.Exchange
                    };

                    ExtendedConfiguration extConfig = (ExtendedConfiguration) Configuration;

                    MonSecurityImbalance secImbalance = new MonSecurityImbalance(sec, Configuration.DecimalRounding,
                                                                                 extConfig.CandleReferencePrice,
                                                                                 CustomImbalanceConfigManager.GetCustomImbalanceConfigs(symbol),
                                                                                 extConfig.StopLossForOpenPositionPct);
                    //1- We add the current security to monitor
                    SecurityImbalancesToMonitor.Add(symbol, secImbalance);

                    Securities.Add(sec);//So far, this is all wehave regarding the Securities

                    //2- We request market data

                    MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MarketDataRequestCounter, sec, SubscriptionRequestType.SnapshotAndUpdates);
                    MarketDataRequestCounter++;
                    OnMessageRcv(wrapper);
                }
            }
        }
        
        protected override bool InsideTradingWindow()
        {
            return DateTime.Now < MarketTimer.GetTodayDateTime(((ExtendedConfiguration)Configuration).MaxOpeningTime);
        }
        
        protected override bool IsTradingTime()
        {
            return DateTime.Now < MarketTimer.GetTodayDateTime(Configuration.ClosingTime);
        }

        protected override bool PacingValidations(string symbol)
        {
            if(SecurityImbalancesToMonitor.ContainsKey(symbol))
                return SecurityImbalancesToMonitor[symbol].ValidPacing(((ExtendedConfiguration)Configuration).MaxMinWaitBtwConsecutivePos);
            else
            {
                return false;
            }
        }

        #endregion
        
        #region Protected OVerriden Methods
        
        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            if (base.Initialize(pOnMessageRcv, pOnLogMsg, configFile))
            {
                 
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion
    }
}