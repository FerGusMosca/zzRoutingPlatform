using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using zHFT.StrategyHandler.SingleStockBuyer.Common.Configuration;
using zHFT.StrategyHandlers.Common.Converters;

namespace zHFT.StrategyHandler.SingleStockBuyer
{
    public class SingleStockBuyer : StrategyBase
    {
        #region Protected Attributes

        protected Common.Configuration.Configuration SSBConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        #endregion

        #region Protected Methods

        protected bool ProcessNewPosition(Contract contract, Wrapper mdWrapper)
        {

            DoLog(string.Format("Single Stock Buyer: Creating position for symbol {0}", contract.Symbol), Constants.MessageType.Information);

            Position pos = new Position()
            {
                Security = new Security()
                        {
                            Symbol = contract.Symbol,
                            MarketData = MarketDataConverter.GetMarketData(mdWrapper, Config),
                            Currency = contract.Currency,
                            SecType = Security.GetSecurityType(contract.SecType)
                        },
                Side = (Side)Convert.ToChar(contract.Side),
                Exchange = contract.Exchange,
                QuantityType = QuantityType.CURRENCY,//We want to buy a certain amount
                PriceType = PriceType.FixedAmount,
                CashQty = contract.Ammount,
                NewPosition = true,
                PosStatus = PositionStatus.PendingNew
            };


            pos.LoadPosId(NextPosId);
            NextPosId++;

            ExecutionSummary summary = new ExecutionSummary()
            {
                Date=DateTime.Now,
                Position = pos,
                Symbol = pos.Security.Symbol,
                AvgPx = null,
                CumQty = 0
            };

            return OpenPositionOnMarket(summary).Success;
        }

        #endregion

        #region Protected Overriden Methods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        //Called when a new position could not be opened by the order router
        //If the reason is that there was not bid or ask we do nothing.
        //If there is another reason, we won't open the position again
        protected override void OnProcessNewPositionCanceled(Position pos)
        {

            if (pos.PositionRejectReason.HasValue)
            {
                if (pos.PositionRejectReason.Value != PositionRejectReason.NoAskAvailable
                    && pos.PositionRejectReason.Value != PositionRejectReason.NoBidAvailable)
                {

                    Contract contract =SSBConfiguration.ContractList.Where(x => x.Symbol == pos.Symbol).FirstOrDefault();

                    if (contract != null)
                        SSBConfiguration.ContractList.Remove(contract);
                }
            }
        
        }

        protected override void OnEvalExecutionSummary(object param)
        {
            try
            {
                lock (tLock)
                {
                    ExecutionSummary summary = (ExecutionSummary)param;

                    //We just handle one stock in this strategy, so there won't be more execution summaries
                    if (summary != null)
                    {
                        if (summary.Position.PositionCleared || summary.Position.PositionCanceledOrRejected)
                        {
                            if (ExecutionSummaries.Values.Where(x => x.Position.PositionCleared || x.Position.PositionCanceledOrRejected).ToList().Count == SSBConfiguration.ContractList.Count)
                            {
                                //Once we closed or canceled all positions we can save @DB
                                SaveAndCleanAllPositions();
                                SSBConfiguration.ContractList.Clear();
                            }
                        }
                    }
                    else
                        throw new Exception(string.Format("@{0}:Could not find Execution Summary for unknown symbol. Cancelling all orders!", StrategyConfiguration.Name));
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0} OnEvalExecutionSummary: Critical error processing execution summary: {1}", SSBConfiguration.Name, ex.Message), Constants.MessageType.Error);
                CancelAllNotCleared();
            }
        }

        protected override CMState ProcessMarketData(Wrapper wrapper)
        {
            lock (tLock)
            {
                string symbol = (string)wrapper.GetField(MarketDataFields.Symbol);

                foreach (Contract contract in SSBConfiguration.ContractList)
                {
                    if(contract.Symbol==symbol)
                    {
                        if (!EvalPositionOpened(symbol))//Position is not opened
                        {
                            return CMState.BuildSuccess(ProcessNewPosition(contract, wrapper), null);
                        }
                        else
                        {

                            if (!EvalPositionCleared(symbol) && !EvalPositionCanceledOrRejected(symbol))
                            {
                                CMState state = OrderRouter.ProcessMessage(wrapper);

                                if (!state.Success)
                                    DoLog(string.Format("Single Stock Buyer: Could not process market data for symbol {0}", symbol), Constants.MessageType.Information);

                                return state;
                            }
                            else
                                return CMState.BuildSuccess();
                        }
                    }
                }
                return CMState.BuildSuccess();
            }
        }

        //All that is necessary was alread initialized at StrategyBase
        protected override bool OnInitialize()
        {
            
            return true;
        }

        #endregion

    }
}
