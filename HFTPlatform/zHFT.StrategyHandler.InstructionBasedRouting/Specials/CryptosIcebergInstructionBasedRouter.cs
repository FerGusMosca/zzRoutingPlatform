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
    //public class CryptosIcebergInstructionBasedRouter : InstructionBasedRouter
    //{

    //    #region Protected Attributes

    //    protected Dictionary<string, IcebergPositionDTO> IcebergPositionInstructions { get; set; }

    //    protected bool AbortRerouting { get; set; }

    //    protected IMarketDataReferenceHandler MarketDataReferenceHandler { get; set; }

    //    #endregion

    //    #region Private Methods

    //    private Position CreateNextPosition(Position prevPos, double qty)
    //    {
    //        Position pos = new Position()
    //        {
    //            Security = new Security()
    //            {
    //                Symbol = prevPos.Symbol,
    //                MarketData = null,
    //                Currency = prevPos.Security.Currency,
    //                SecType = prevPos.Security.SecType
    //            },
    //            Side = prevPos.Side,
    //            PriceType = PriceType.FixedAmount,
    //            NewPosition = true,
    //            PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
    //            AccountId = prevPos.AccountId,
    //            CashQty = prevPos.Side == zHFT.Main.Common.Enums.Side.Buy ? qty : (double?)null,//En las compras tenemos el monto en BTC <quote currency>, en las ventas en unidades de la moneda vendida
    //            Qty = prevPos.Side == zHFT.Main.Common.Enums.Side.Buy ? (double?)null : qty,//En las compras tenemos el monto en BTC <quote currency>, en las ventas en unidades de la moneda vendida
    //            QuantityType = prevPos.Side == zHFT.Main.Common.Enums.Side.Buy ? QuantityType.CURRENCY : QuantityType.CRYPTOCURRENCY,

    //        };

    //        return pos;
    //    }

    //    private Position CreateNextPosition(Instruction instr,zHFT.Main.Common.Enums.Side side, double qty)
    //    {
    //        Position pos = new Position()
    //        {
    //            Security = new Security()
    //            {
    //                Symbol = instr.Symbol,
    //                MarketData = null,
    //                Currency = instr.Account.Currency,
    //                SecType = instr.SecurityType
    //            },
    //            Side = side,
    //            PriceType = PriceType.FixedAmount,
    //            NewPosition = true,
    //            PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
    //            AccountId = instr.Account != null ? instr.Account.GenericAccountNumber : null,
    //            CashQty = side == zHFT.Main.Common.Enums.Side.Buy ? qty : (double?)null,//En las compras tenemos el monto en BTC <quote currency>, en las ventas en unidades de la moneda vendida
    //            Qty = side == zHFT.Main.Common.Enums.Side.Buy ? (double?)null : qty,//En las compras tenemos el monto en BTC <quote currency>, en las ventas en unidades de la moneda vendida
    //            QuantityType = side == zHFT.Main.Common.Enums.Side.Buy ? QuantityType.CURRENCY : QuantityType.CRYPTOCURRENCY,

    //        };

    //        return pos;
    //    }

    //    private ExecutionSummary CreateInitialExecutionSummary(Position pos)
    //    {

    //        ExecutionSummary summary = new ExecutionSummary()
    //        {
    //            Date = DateTime.Now,
    //            Position = pos,
    //            Symbol = pos.Security.Symbol,
    //            AvgPx = null,
    //            CumQty = 0
    //        };

    //        return summary;
    //    }

    //    private IcebergPositionDTO CreateInitialIcebergPositionDTO(Instruction instr, zHFT.Main.Common.Enums.Side side, Position pos,
    //                                                               double stepAmmountQuoteCurrency)
    //    {
    //        double totalAmmountInSummaryCurrency = GetInstrAmmountInSummaryCurrency(instr);
    //        double stepAmmountInSummaryCurrency = totalAmmountInSummaryCurrency / instr.GetSteps();

    //        IcebergPositionDTO newIcebergPosition = new IcebergPositionDTO()
    //        {
    //            Instruction = instr,
    //            CumAmmount = 0,
    //            LeavesAmmount = totalAmmountInSummaryCurrency,
    //            TotalAmmount = totalAmmountInSummaryCurrency,
    //            QuoteCurrencyLeavesAmmount = Convert.ToDouble(instr.Ammount.Value),
    //            QuoteCurrencyTotalAmmount = Convert.ToDouble(instr.Ammount.Value),
    //            Positions = new List<Position>(),
    //            PositionStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
    //            Side = side,
    //            CurrentStep = 1,
    //            CurrentStepAmmount = stepAmmountInSummaryCurrency,
    //            StepAmmount = stepAmmountInSummaryCurrency,
    //            QuoteCurrencyCurrentStepAmmount = stepAmmountQuoteCurrency,
    //            QuoteCurrencyStepAmmount = stepAmmountQuoteCurrency,
    //        };

    //        newIcebergPosition.Positions.Add(pos);

    //        return newIcebergPosition;
    //    }

    //    private void UpdateIcebergPositionDTO(IcebergPositionDTO icebergPosition, ExecutionSummary prevSummary)
    //    {
    //        icebergPosition.CumAmmount += prevSummary.CumQty;//Algo se llegó a ejecutar? -> lo cargamos
    //        icebergPosition.LeavesAmmount = icebergPosition.TotalAmmount - icebergPosition.CumAmmount;
    //    }

    //    private void UpdateIcebergPositionDTO(IcebergPositionDTO icebergPosition, double newQty, ExecutionSummary prevSummary,
    //                                            Position newPos,Instruction instr)
    //    { 
    //        icebergPosition.CurrentStepAmmount = newQty;
    //        icebergPosition.Positions.Add(newPos);
    //        icebergPosition.PositionStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew;
    //    }

    //    private void CleanPosition(ExecutionSummary summary)
    //    {
    //        PositionInstructions.Remove(summary.Position.Symbol);
    //        IcebergPositionInstructions.Remove(summary.Position.Symbol);
    //        ExecutionSummaries.Remove(summary.Position.Symbol);
    //        Positions.Remove(summary.Position.Symbol);
    //        UnsuscribeMarketData(summary.Position);
    //    }


    //    #endregion

    //    #region Protected Methods

    //    protected override void DoClear()
    //    {
    //        PositionInstructions.Clear();
    //        ExecutionSummaries.Clear();
    //        Positions.Clear();
    //        IcebergPositionInstructions.Clear();
    //    }

    //    protected override bool EvalMarketData(ExecutionSummary summary)
    //    {
    //        return true;
    //    }

    //    protected double GetInstrAmmountInSummaryCurrency(Instruction instr)
    //    {
    //        if (instr.InstructionType.Type == InstructionType.GetNewPosInstr().Type)
    //        {
    //            MarketData md = MarketDataReferenceHandler.GetMarketData(instr.Symbol);

    //            if (!instr.Ammount.HasValue)
    //                throw new Exception("Missing instruction ammount on processing new buy position");

    //            if (!md.Trade.HasValue)
    //                throw new Exception(string.Format("There is not a market data last trade for symbol {0} and quote currency", instr.Symbol));

    //            double ammountInSummaryCurrency = Convert.ToDouble(instr.Ammount.Value) / md.Trade.Value;

    //            return ammountInSummaryCurrency;

    //        }
    //        else if (instr.InstructionType.Type == InstructionType.GetUnwindPos().Type)
    //        {
    //            if (!instr.Ammount.HasValue)
    //                throw new Exception("Missing instruction ammount on processing new sell position");

    //            return Convert.ToDouble(instr.Ammount.Value);
    //        }
    //        else
    //            throw new Exception(string.Format("Instruction type not valid: {0}", instr.InstructionType.Type));
    //    }

    //    protected override void ProcessNewPosition(Instruction instr)
    //    {
    //        DoLog(string.Format("{0}: Creating position for symbol {1}", IBRConfiguration.Name, instr.Symbol), Constants.MessageType.Information);

    //        double stepAmmount = Convert.ToDouble(instr.Ammount) / instr.GetSteps();

    //        AbortRerouting = false;

    //        Position pos = CreateNextPosition(instr, zHFT.Main.Common.Enums.Side.Buy, stepAmmount);

    //        pos.LoadPosId(NextPosId);
    //        NextPosId++;

    //        if (pos != null)
    //        {
    //            ExecutionSummary summary =  CreateInitialExecutionSummary(pos);

    //            IcebergPositionDTO newIcebergPosition = CreateInitialIcebergPositionDTO(instr, zHFT.Main.Common.Enums.Side.Buy, pos,
    //                                                                                    stepAmmount);

    //            newIcebergPosition.Positions.Add(pos);
    //            ExecutionSummaries.Add(pos.Security.Symbol, summary);
    //            PositionInstructions.Add(pos.Security.Symbol, instr);
    //            IcebergPositionInstructions.Add(pos.Security.Symbol, newIcebergPosition);
    //        }

    //    }

    //    protected void ReRouteCurrentStep(Instruction instr, ExecutionSummary prevSummary, IcebergPositionDTO icebergPosition)
    //    {
    //        DoLog(string.Format("{0}: Rerouting step {2} position for symbol {1}", IBRConfiguration.Name, instr.Symbol, icebergPosition.CurrentStep), Constants.MessageType.Information);

    //        Position currentPos = Positions[instr.Symbol];

    //        if (currentPos != null)
    //        {
    //            UpdateIcebergPositionDTO(icebergPosition, prevSummary);

    //            double nextStepAmmount = icebergPosition.CalculateNextStepAmmountInQuoteCurrency();
    //            icebergPosition.CurrentStepAmmount = nextStepAmmount;

    //            Position nextPos = CreateNextPosition(currentPos, nextStepAmmount);
    //            icebergPosition.Positions.Add(nextPos);
    //            icebergPosition.PositionStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew;

    //            nextPos.LoadPosId(NextPosId);
    //            NextPosId++;

    //            ExecutionSummary nextSummary = CreateInitialExecutionSummary(nextPos);

    //            ExecutionSummaries.Remove(currentPos.Security.Symbol);
    //            ExecutionSummaries.Add(currentPos.Security.Symbol, nextSummary);

    //            Positions.Remove(instr.Symbol);//Con esto me aseguro que se pueda abrir la próxima posición del step n+1
    //        }
    //        else
    //            DoLog(string.Format("{0}: Could not re route failed step {2} position for symbol {1} because could not find the positions on Positions collection", 
    //                                IBRConfiguration.Name, instr.Symbol, icebergPosition.CurrentStep), Constants.MessageType.Information);
        
    //    }


    //    protected void ProcessNextNewPosition(Instruction instr,ExecutionSummary prevSummary, IcebergPositionDTO icebergPosition)
    //    {
    //        icebergPosition.CurrentStep++;

    //        DoLog(string.Format("{0}: Creating step {2} position for symbol {1}", IBRConfiguration.Name, instr.Symbol,icebergPosition.CurrentStep), Constants.MessageType.Information);

    //        UpdateIcebergPositionDTO(icebergPosition, prevSummary);//Lo necesito para que el icebergDTO tenga lo última de la última ejecución --> luego calculo el nextStepAmmount

    //        double nextStepQty = icebergPosition.CalculateNextStepAmmountInSummaryCurrency();

    //        Position pos = CreateNextPosition(instr, zHFT.Main.Common.Enums.Side.Buy, nextStepQty);

    //        pos.LoadPosId(NextPosId);
    //        NextPosId++;

    //        ExecutionSummary nextSummary = CreateInitialExecutionSummary(pos);

    //        UpdateIcebergPositionDTO(icebergPosition, nextStepQty, prevSummary, pos, instr);

    //        ExecutionSummaries.Remove(pos.Security.Symbol);
    //        ExecutionSummaries.Add(pos.Security.Symbol, nextSummary);

    //        Positions.Remove(instr.Symbol);//Con esto me aseguro que se pueda abrir la próxima posición del step n+1
    //    }

    //    protected override void ProcessUnwindPosition(Instruction instr, AccountPosition portfPos)
    //    {

    //        DoLog(string.Format("{0}: Unwinding position for symbol {1}", IBRConfiguration.Name, instr.Symbol), Constants.MessageType.Information);

    //        double nextStepQty = Convert.ToDouble(instr.Ammount.Value) / instr.GetSteps();

    //        AbortRerouting = false;

    //        Position pos = CreateNextPosition(instr, zHFT.Main.Common.Enums.Side.Sell, nextStepQty);

    //        pos.LoadPosId(NextPosId);
    //        NextPosId++;

    //        if (pos != null)
    //        {
    //            ExecutionSummary summary = CreateInitialExecutionSummary(pos);

    //            IcebergPositionDTO newIcebergPosition = CreateInitialIcebergPositionDTO(instr, zHFT.Main.Common.Enums.Side.Sell, pos,
    //                                                                                    nextStepQty);

    //            newIcebergPosition.Positions.Add(pos);
    //            ExecutionSummaries.Add(pos.Security.Symbol, summary);
    //            PositionInstructions.Add(pos.Security.Symbol, instr);
    //            IcebergPositionInstructions.Add(pos.Security.Symbol, newIcebergPosition);
    //        }
    //    }

    //    protected override bool OnInitialize()
    //    {

    //        IcebergPositionInstructions = new Dictionary<string, IcebergPositionDTO>();

    //        AbortRerouting = false;

    //        var marketDataReferenceHandler = Type.GetType(IBRConfiguration.MarketDataReferenceHandler);
    //        if (marketDataReferenceHandler != null)
    //            MarketDataReferenceHandler = (IMarketDataReferenceHandler)Activator.CreateInstance(marketDataReferenceHandler, OnLogMsg, IBRConfiguration.AccountManagerConfig);
    //        else
    //        {
    //            DoLog("assembly not found: " + IBRConfiguration.MarketDataReferenceHandler, Main.Common.Util.Constants.MessageType.Error);
    //            return false;
    //        }

    //        return base.OnInitialize();
        
    //    }

    //    protected bool ProcessNewIcebergPositionCleared(Instruction instr, ExecutionSummary summary,  IcebergPositionDTO icebergDTO)
    //    {
    //        instr.Text += string.Format("Step {0}: {1} - ", icebergDTO.CurrentStep, summary.Text);

    //        if (instr.IsMerge)//ya estaba online, actualzamos las shares
    //            instr.AccountPosition.Ammount += Convert.ToDecimal(summary.CumQty);
    //        else
    //            instr.AccountPosition.Ammount = Convert.ToDecimal(summary.CumQty);

    //        if (icebergDTO.CurrentStep == instr.Steps)
    //        {
    //            instr.Executed = true;
    //            instr.AccountPosition.PositionStatus = InstructionBasedRouting.BusinessEntities.PositionStatus.GetNewPositionStatus(true);
    //        }
    //        else
    //        {
    //            instr.Executed = false;
    //            //El status queda igual
                
    //        }

    //        return true;

    //    }

    //    protected bool ProcessNewIcebergPositionCanceledOrRejected(Instruction instr, ExecutionSummary summary, IcebergPositionDTO icebergDTO)
    //    {
    //        instr.Text += string.Format("Step {0}: Buy Execution canceled or rejected: {1} - ", icebergDTO.CurrentStep, summary.Text);
    //        instr.Executed = false;
    //        //El status queda igual
    //        return false;

    //    }

    //    protected bool ProcessUnwindIcebergPositionCleared(Instruction instr, ExecutionSummary summary, IcebergPositionDTO icebergDTO)
    //    {
    //        if (instr.IsFromUnwindAll)
    //        {
    //            instr.AccountPosition.PositionStatus = InstructionBasedRouting.BusinessEntities.PositionStatus.GetOfflineUnwindedStatus();
    //            instr.AccountPosition.Active = false;
    //        }
    //        else if (instr.Steps == icebergDTO.CurrentStep)//Estamos en el último paso
    //        {
    //            instr.AccountPosition.PositionStatus = InstructionBasedRouting.BusinessEntities.PositionStatus.GetOfflineUnwindedStatus();
    //            instr.AccountPosition.Active = false;
                

    //        }
    //        else //currentStep < Steps
    //        {

    //            instr.AccountPosition.PositionStatus = InstructionBasedRouting.BusinessEntities.PositionStatus.GetNewPositionStatus(true);
    //            instr.AccountPosition.Ammount -= instr.Ammount;
    //            instr.AccountPosition.Active = true;
    //        }

    //        return true;
    //    }

    //    protected bool ProcessUnwindIcebergPositionCanceledOrRejected(Instruction instr, ExecutionSummary summary, IcebergPositionDTO icebergDTO)
    //    {
    //        instr.Text += string.Format("Step {0}: Unwind execution canceled or rejected: {1} - ", icebergDTO.CurrentStep, summary.Text);
    //        instr.Executed = false;
    //        //El status queda igual
    //        return false;
    //    }

    //    protected bool ProcessIcebergInstructionExecuted(Instruction instr, ExecutionSummary summary,  IcebergPositionDTO icebergDTO)
    //    {
    //        if (instr.InstructionType.Type == InstructionType._NEW_POSITION)
    //        {
    //            if (summary.Position.PositionCleared)
    //            {
    //                return ProcessNewIcebergPositionCleared(instr, summary, icebergDTO);
    //            }
    //            else if (summary.Position.PositionCanceledOrRejected)
    //            {
    //                return ProcessNewIcebergPositionCanceledOrRejected(instr, summary, icebergDTO);
    //            }
    //            else
    //                throw new Exception(string.Format("New Position: Invalid state for position for symbol {0} @OnEvalExecutionSummary", summary.Position.Symbol));
    //        }
    //        else if (instr.InstructionType.Type == InstructionType._UNWIND_POSITION)
    //        {
    //            if (summary.Position.PositionCleared)
    //            {
    //                return ProcessUnwindIcebergPositionCleared(instr, summary, icebergDTO);
    //            }
    //            else if (summary.Position.PositionCanceledOrRejected)
    //            {
    //                return ProcessUnwindIcebergPositionCanceledOrRejected(instr, summary, icebergDTO);
    //            }
    //            else
    //                throw new Exception(string.Format("Unwinding Position: Invalid state for position for symbol {0} @OnEvalExecutionSummary", summary.Position.Symbol));
    //        }
    //        else
    //            throw new Exception(string.Format("@{0}: Invalid instrction type to process: {1}", IBRConfiguration.Name, instr.InstructionType.Type.ToString()));

           
    //    }

    //    protected override void CancelAllNotCleared(int? accountNumber=null)
    //    {
    //        AbortRerouting = true;

    //        base.CancelAllNotCleared(accountNumber);
    //    }

    //    protected bool IsFinalStep(ExecutionSummary summary, IcebergPositionDTO icebergDTO, Instruction instr)
    //    {
    //        if (summary.Position.PositionCanceledOrRejected)
    //            return false;
    //        else
    //        {
    //            if (summary.IsFilledPosition() && summary.CumQty <= icebergDTO.StepAmmount)//Si se ejecutó algo menor o igual al monto del step y fue filled, no hay nada mas que descargar
    //                return true;
    //            else if (summary.IsFilledPosition() && instr.Steps == 1)//Cuando tenemos un solo step, resolvemos por la fácil
    //                return true;
    //            else
    //                return false;
    //        }
    //    }

    //    protected override void OnEvalExecutionSummary(object param)
    //    {
    //        try
    //        {
    //            lock (tLock)
    //            {
    //                ExecutionSummary summary = (ExecutionSummary)param;

    //                if (summary != null)
    //                {

    //                    if (summary.IsFilledPosition() || summary.Position.PositionCanceledOrRejected)
    //                    {

    //                        summary.Position.PositionCleared = summary.LeavesQty <= 0;

    //                        if (    PositionInstructions.ContainsKey(summary.Position.Symbol)
    //                             && IcebergPositionInstructions.ContainsKey(summary.Position.Symbol))
    //                        {

    //                            Instruction instr = PositionInstructions[summary.Position.Symbol];
    //                            IcebergPositionDTO icebergDTO = IcebergPositionInstructions[summary.Position.Symbol];

    //                            if (ProcessIcebergInstructionExecuted(instr, summary, icebergDTO))
    //                            {//Se terminó la posición completa

    //                                if (IsFinalStep(summary,icebergDTO,instr))//Estabamos en el último step
    //                                {
    //                                    CleanPosition(summary);
    //                                    InstructionManager.Persist(instr);
    //                                    SaveExecutionSummary(summary, (int?)IBRConfiguration.AccountNumber);
    //                                }
    //                                else
    //                                { 
    //                                    //Calculamos y Ruteamos el siguiente step
    //                                    ProcessNextNewPosition(instr, summary, icebergDTO);
    //                                }
    //                            }
    //                            else
    //                            {
    //                                if (summary.Position.PositionCanceledOrRejected)
    //                                {
    //                                    //Re Ruteamos el current step
    //                                    if (!AbortRerouting)
    //                                    {
    //                                        ReRouteCurrentStep(instr, summary, icebergDTO);
    //                                        SaveExecutionSummary(summary, (int?)IBRConfiguration.AccountNumber);
    //                                    }
    //                                    else
    //                                    {   //Aca si se canceló todo 
    //                                        CleanPosition(summary);

    //                                        if (IcebergPositionInstructions.Count == 0)
    //                                            AbortRerouting = false;

    //                                        InstructionManager.Persist(instr);
    //                                        SaveExecutionSummary(summary, (int?)IBRConfiguration.AccountNumber);
    //                                    }

                                       
    //                                }
    //                            }
    //                        }
    //                        else
    //                        {
    //                            DoLog(string.Format("@{0} OnEvalExecutionSummary: Position already processed: {1}.Filled={2},CancelOrRejecter={3}",
    //                                                        IBRConfiguration.Name, summary.Position.Symbol,
    //                                                        summary.IsFilledPosition(),
    //                                                        summary.Position.PositionCanceledOrRejected),
    //                                                        Constants.MessageType.Information);

    //                        }
    //                    }
    //                    else
    //                        SaveExecutionSummary(summary, (int?)IBRConfiguration.AccountNumber);
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            DoLog(string.Format("@{0} OnEvalExecutionSummary: Critical error processing execution summary: {1}", IBRConfiguration.Name, ex.Message), Constants.MessageType.Error);
    //            CancelAllNotCleared();
    //        }
    //    }

    //    #endregion
    //}
}
