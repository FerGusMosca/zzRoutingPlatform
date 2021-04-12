using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.LogicLayer;
using zHFT.StrategyHandler.MomentumPortfolios.BusinessEntities;
using zHFT.StrategyHandler.MomentumPortfolios.Common.Configuration;
using zHFT.StrategyHandler.MomentumPortfolios.Common.Enums;
using zHFT.StrategyHandler.MomentumPortfolios.DataAccessLayer.Managers;
using zHFT.StrategyHandlers.Common.Converters;
using Position = zHFT.StrategyHandler.MomentumPortfolios.BusinessEntities.Position;

namespace zHFT.StrategyHandler.MomentumPortfolios.StrategyHandler
{
    public class MomentumStrategyPortfolioFormation : StrategyBase
    {
        #region Protected Attributes

        protected StrategyManager StrategyManager { get; set; }

        protected Thread PortfolioTrhead { get; set; }

        protected double CumPortfolioSize { get; set; }

        protected Common.Configuration.Configuration MomentumConfiguration 
        { 
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        #endregion

        #region Potected and Private Methods

        protected override void UnsuscribeMarketData(Main.BusinessEntities.Positions.Position pos)
        {
            throw new NotImplementedException();
        }

        protected void RunPortfolioFormation()
        {
            lock (tLock) 
            {
                try
                {
                    Strategy strategy = StrategyManager.GetStrategyByConfig(MomentumConfiguration.Date.HasValue ? MomentumConfiguration.Date.Value : DateTime.Now,
                                                                             MomentumConfiguration.StocksInPortfolio,
                                                                             MomentumConfiguration.HoldingMonths,
                                                                             (Weight)MomentumConfiguration.Weight,
                                                                             (FilterStocks)MomentumConfiguration.FilterStocks,
                                                                             (Ratio)MomentumConfiguration.Ratio);
                    if (strategy.Portfolios != null && strategy.Portfolios.Count > 0)
                    {
                        Portfolio portfolo = strategy.Portfolios.FirstOrDefault();

                        
                        foreach (BusinessEntities.Position pos in portfolo.Positions.Take(MomentumConfiguration.PositionsToProcess))
                        {
                            zHFT.Main.BusinessEntities.Positions.Position posToMarket = new Main.BusinessEntities.Positions.Position();
                            posToMarket.Security = new Security() { Symbol = pos.Stock.Ticker };
                            posToMarket.LoadPosId(NextPosId);
                            posToMarket.Side = MomentumConfiguration.Side.HasValue ? (zHFT.Main.Common.Enums.Side)Convert.ToChar(MomentumConfiguration.Side.Value) : zHFT.Main.Common.Enums.Side.Buy;
                            posToMarket.Exchange = pos.Stock.Market;
                            posToMarket.QuantityType = QuantityType.CURRENCY;//We want to buy a certain amount
                            posToMarket.PriceType = PriceType.FixedAmount;
                            posToMarket.CashQty = MomentumConfiguration.PortfolioCashSize * pos.Weight;
                            posToMarket.NewPosition = true;
                            posToMarket.PosStatus = PositionStatus.PendingNew;
                            posToMarket.PositionCleared = false;
                            posToMarket.PositionCanceledOrRejected = false;

                            NextPosId++;
                            Positions.Add(posToMarket.Symbol, posToMarket);
                            DoLog(string.Format("Creating Position: Stock:{0} - Weight:{1}", pos.Stock.Ticker, pos.Weight), Constants.MessageType.Information);
                        }
                    }
                    else
                        throw new Exception("Could not find a portfolio in strategy " + strategy.Id);


                }
                catch (Exception ex)
                {
                    DoLog("Critical Error processing portfolio formation! : " + ex.Message, Constants.MessageType.Error);
                
                }
            }
        }

        private bool ProcessNewPosition(zHFT.Main.BusinessEntities.Positions.Position pos, Wrapper marketDataWrapper)
        {
            pos.Security.MarketData = MarketDataConverter.GetMarketData(marketDataWrapper, Config);
            pos.Security.Currency = Currency.USD.ToString();//we only work with USD securities
            pos.Security.SecType = SecurityType.CS;//we only work with CS security type

            ExecutionSummary summary = new ExecutionSummary()
            {
                Date= DateTime.Now,
                Position = pos,
                Symbol = pos.Security.Symbol,
                AvgPx = null,
                CumQty = 0
            };

            return OpenPositionOnMarket(summary).Success;
        }

        #endregion

        #region Protected Abstract Methods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        //Called when a new position could not be opened by the order router
        //If the reason is that there was not bid or ask we prepare the Positions collection to re open the position
        //If there is another reason, we don't do nothing so nothing is re opened
        protected override void OnProcessNewPositionCanceled(zHFT.Main.BusinessEntities.Positions.Position pos)
        {
            if (pos.PositionRejectReason.HasValue)
            {
                if (pos.PositionRejectReason.Value == PositionRejectReason.NoAskAvailable
                    || pos.PositionRejectReason.Value == PositionRejectReason.NoBidAvailable)
                {
                    //we need to prepare the position to be re opened on new market data info
                    pos.NewDomFlag = false;
                    pos.PositionCleared = false;
                    pos.PositionCanceledOrRejected = false;
                    pos.PosStatus = PositionStatus.PendingNew;
                    pos.PositionRejectReason = null;
                    Positions.Add(pos.PosId, pos);
                }
            }
        }

        protected override CMState ProcessMarketData(Wrapper wrapper)
        {
            lock (tLock)
            {
                string symbol = Convert.ToString(wrapper.GetField(MarketDataFields.Symbol));

                if (Positions.ContainsKey(symbol))
                {
                    zHFT.Main.BusinessEntities.Positions.Position posToOpen = Positions.Values.Where(x => x.Security.Symbol == symbol).FirstOrDefault();

                    if (!EvalPositionOpened(posToOpen.Symbol))
                    {
                        return CMState.BuildSuccess(ProcessNewPosition(posToOpen, wrapper),null);
                    }
                    else //Already opened position
                    {
                        if (!EvalPositionCleared(posToOpen.Symbol) && !EvalPositionCanceledOrRejected(posToOpen.Symbol))
                        {
                            CMState state = OrderRouter.ProcessMessage(wrapper);

                            if (!state.Success)
                                DoLog(string.Format("{0}: Could not process market data for symbol {1}", MomentumConfiguration.Name, symbol), Constants.MessageType.Information);

                            return state;
                        }
                        else
                            return CMState.BuildSuccess();
                    }
                }
                else
                    return CMState.BuildSuccess();
            }
        }

        protected override bool OnInitialize()
        {
            StrategyManager = new StrategyManager(MomentumConfiguration.ConnectionString);
            PortfolioTrhead = new Thread(new ThreadStart(RunPortfolioFormation));
            PortfolioTrhead.Start();
            
            CumPortfolioSize = 0;
            return true;
        }

        protected override void OnEvalExecutionSummary(object param)
        {
            try
            {
                
                lock (tLock)
                {
                    ExecutionSummary summary = (ExecutionSummary)param;
                    if (summary != null)
                    {
                        DoLog(string.Format("@{0} OnEvalExecutionSummary for symbol {1}", MomentumConfiguration.Name, summary.Position.Symbol), Constants.MessageType.Information);

                        if (summary.LeavesQty <= 0 && !summary.Position.PositionCanceledOrRejected)
                            summary.Position.PositionCleared = true;

                        CumPortfolioSize = 0;
                        ExecutionSummaries.Values.ToList().ForEach(x => CumPortfolioSize += x.GetCashExecution());

                        if (CumPortfolioSize >= MomentumConfiguration.PortfolioCashSize)
                        {
                            DoLog(string.Format("@{0} OnEvalExecutionSummary saving and cleaning positions for reaching max portfolio cash size with symbol {1}", MomentumConfiguration.Name, summary.Position.Symbol), Constants.MessageType.Information);
                            SaveAndCleanAllPositions();
                        }

                        else if (Positions.Values.Where(x => x.PositionCleared || x.PositionCanceledOrRejected).Count() == MomentumConfiguration.PositionsToProcess)
                        {
                            DoLog(string.Format("@{0} OnEvalExecutionSummary saving and cleaning positions for reaching max positions cleared and rejected with symbol {1}", MomentumConfiguration.Name, summary.Position.Symbol), Constants.MessageType.Information);
                            SaveAndCleanAllPositions();
                        }
                        else
                            SaveExecutionSummaries();

                    }
                    else
                        throw new Exception(string.Format("@{0}:Could not find Execution Summary for unknown symbol. Cancelling all orders!", StrategyConfiguration.Name));
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0} OnEvalExecutionSummary: Critical error processing execution summary: {1}", MomentumConfiguration.Name, ex.Message), Constants.MessageType.Error);
                CancelAllNotCleared();
            }
        }

        #endregion

    }
}
