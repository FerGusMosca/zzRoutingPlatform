using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;
using zHFT.StrategyHandler.InstructionBasedRouting.DataAccessLayer.Managers;
using zHFT.StrategyHandler.LogicLayer;

namespace zHFT.StrategyHandler.InstructionBasedRouting
{
    public class InstructionBasedRouter : StrategyBase
    {
        #region Protected Attributes

        protected Thread ProcessInstructionsThread { get; set; }

        protected IInstructionManagerAccessLayer InstructionManager { get; set; }

        protected IAccountManagerAccessLayer AccountManager { get; set; }

        protected IPositionManagerAccessLayer PositionManager { get; set; }

        protected IAccountReferenceHandler AccountReferenceHandler { get; set; }

        protected Dictionary<string, Instruction> PositionInstructions { get; set; }

        #endregion

        #region Protected Attributes

        protected Common.Configuration.Configuration IBRConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        #endregion

        #region Protected Methods

        protected virtual void DoClear()
        {
            PositionInstructions.Clear();
            ExecutionSummaries.Clear();
            Positions.Clear();
        }

        protected void ProcessPositionsCleaningSync(List<Instruction> instructionsToCleanPositions)
        {
            //If we have at least one instructionToCleanPosition
            if (instructionsToCleanPositions.Count > 0)
            {
                CancelAllNotCleared((int?)IBRConfiguration.AccountNumber);
                foreach (Instruction instr in instructionsToCleanPositions)
                {
                    instr.Executed = true;
                    InstructionManager.Persist(instr);
                    
                }

                DoClear();
            }
        }

        protected void ProcessAccounBalanceSync(List<Instruction> instructionsBalanceSync)
        {
            foreach (Instruction instr in instructionsBalanceSync)
            {
                try
                {
                    if (instr.Account.AccountNumber == IBRConfiguration.AccountNumber)
                    {
                        Account account = AccountManager.GetById(instr.Account.Id);
                        AccountReferenceHandler.SyncAccountBalance(account);

                        if (!AccountReferenceHandler.ReadyAccountSummary())
                        {
                            Account accountToSync = AccountReferenceHandler.GetAccountToSync();
                            AccountManager.Persist(accountToSync);
                            DoLog(string.Format("Account {0} balance succesfully synchronized", accountToSync.BrokerAccountName), Main.Common.Util.Constants.MessageType.Information);
                            instr.Executed = true;
                            InstructionManager.Persist(instr);
                        }
                    }
                    else
                        DoLog(string.Format("Discarding account balance sync for account number: {0}", instr.Account.AccountNumber), Main.Common.Util.Constants.MessageType.Information);

                }
                catch (Exception ex)
                {
                    DoLog(string.Format("Critical error processing account sync instruction: {0} - {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : ""), Main.Common.Util.Constants.MessageType.Error);
                }
            }
        }

        protected bool ValidateRelatedInstructionToUnwind(Instruction relInstr)
        {
            AccountPosition pos = PositionManager.GetActivePositionBySymbol(relInstr.Symbol,relInstr.Account.Id);
        
            //we check that the position exists
            if (pos != null)
            {
                relInstr.AccountPosition = pos;
                return true;
            }
            else
            {
                relInstr.AccountPosition = null;
                return false;
            }
        }

        protected bool IsUnwindedPosition(Instruction relInstr, IList<AccountPosition> positions)
        {
            string instrSymbol = relInstr.Symbol;
            if (!positions.Any(x => x.Security.Symbol == instrSymbol))
            {
                return relInstr.AccountPosition.PositionStatus.Code == InstructionBasedRouting.BusinessEntities.PositionStatus._SENT_TO_UNWIND;
            }
            else
            {
                AccountPosition pos = positions.Where(x => x.Security.Symbol == instrSymbol).FirstOrDefault();

                if (pos == null || pos.Shares == 0)
                    return true;
                else
                    return false;
            }
        
        }

        private void MarkAsNotEnoughMoney(ref Instruction relInstr)
        {
            relInstr.Executed = true;
            relInstr.Text = "No tiene suficiente dinero asignado para armar la posición";

            if (relInstr.AccountPosition != null && relInstr.AccountPosition.PositionStatus != null)
                relInstr.AccountPosition.PositionStatus = InstructionBasedRouting.BusinessEntities.PositionStatus.GetNewPositionStatus(false);

            InstructionManager.Persist(relInstr);

            DoLog(string.Format("@{1} - The money assigned for position on symbol {0} is not enough",
                          relInstr.Symbol, IBRConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);

        }

        private void MarkAsAlreadyUnwinded(ref Instruction relInstr)
        {
            relInstr.Executed = true;
            relInstr.Text = "La posición que desea descargar ya se encuentra descargada";
            
            if(relInstr.AccountPosition!=null && relInstr.AccountPosition.PositionStatus!=null)
                relInstr.AccountPosition.PositionStatus = InstructionBasedRouting.BusinessEntities.PositionStatus.GetOfflineUnwindedStatus();
            
            InstructionManager.Persist(relInstr);

            DoLog(string.Format("@{1} - The position for symbol {0} was already unwinded",
                          relInstr.Symbol, IBRConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
        
        }

        private void MarkAsUnwindingInProgress(ref Instruction relInstr)
        {
            relInstr.Executed = true;
            relInstr.Text = "La posición que desea descargar ya se siendo descargada";

            InstructionManager.Persist(relInstr);

            DoLog(string.Format("@{1} - The position for symbol {0} was already being unwindeds",
                          relInstr.Symbol, IBRConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
        }

        protected void ProcessNewPositionSync(ref Instruction relInstr,IList<AccountPosition> positions,bool unwindAll)
        {
            relInstr.AccountPosition = PositionManager.GetActivePositionBySymbol(relInstr.Symbol,relInstr.Account.Id);
            string instrSymbol = relInstr.Symbol;

            if (relInstr.InstructionType.Type == InstructionType._NEW_POSITION)
            {
                relInstr.IsOnlinePosition = positions.Any(x => x.Security.Symbol == instrSymbol);
                relInstr.IsFromUnwindAll = false;
                ProcessNewPosition(relInstr);
            }
            else if (relInstr.InstructionType.Type == InstructionType._UNWIND_POSITION)
            {
                relInstr.AccountPosition = PositionManager.GetById(relInstr.AccountPosition.Id);
                if (ValidateRelatedInstructionToUnwind(relInstr))//we check the positions exists in our database
                {
                    if (IsUnwindedPosition(relInstr, positions))//we check the position exists in the market
                    {
                        MarkAsAlreadyUnwinded(ref relInstr);
                        return;
                    }
                    else
                    {
                        AccountPosition prevPos = positions.Where(x => x.Security.Symbol == instrSymbol).FirstOrDefault();
                        relInstr.IsOnlinePosition = positions.Any(x => x.Security.Symbol == instrSymbol);
                        relInstr.IsFromUnwindAll = unwindAll;
                        ProcessUnwindPosition(relInstr, prevPos);
                    }
                }
                else
                    MarkAsAlreadyUnwinded(ref relInstr);
            }
            else
                DoLog(string.Format("@{2} - Could not run related instruction for type {1} in Account {0} ",
                      IBRConfiguration.AccountNumber, relInstr.InstructionType.Type, IBRConfiguration.Name),
                      Main.Common.Util.Constants.MessageType.Information);
        
        }

        //
        protected void ProcessNewPositionsNoSync(List<Instruction> instructionsPositionsSync)
        {
            foreach (Instruction instr in instructionsPositionsSync)
            {
                //We have a sync instrx but we won't try to sync again
                if (instr.RelatedInstruction != null)//Tenemos una instrx individual
                {
                    //RECOVER RELATED INSTRUCTION
                    instr.RelatedInstruction.AccountPosition = PositionManager.GetActivePositionBySymbol(instr.RelatedInstruction.Symbol,instr.Account.Id);

                    if (instr.RelatedInstruction.InstructionType.Type == InstructionType._NEW_POSITION)
                    {
                        instr.Executed = true;
                        instr.Text = "No ejecutada por existencia de otras sincronizaciones";
                        //instr.RelatedInstruction.Text = "Ejecutada sin sincronización";
                        instr.RelatedInstruction.IsOnlinePosition = instr.RelatedInstruction.AccountPosition.PositionStatus.IsOnline();
                        ProcessNewPosition(instr.RelatedInstruction);
                        InstructionManager.Persist(instr);
                        InstructionManager.Persist(instr.RelatedInstruction);
                    }
                    else if (instr.RelatedInstruction.InstructionType.Type == InstructionType._UNWIND_POSITION)
                    {
                        IList<AccountPosition> onlinePositions = GetCurrentPositionsInMarket(instr.Account);

                        if (onlinePositions != null
                            && onlinePositions.Any(x => x.Security.Symbol == instr.RelatedInstruction.AccountPosition.Security.Symbol)//La posición esta online
                            && !PositionInstructions.Keys.Any(x => x == instr.RelatedInstruction.AccountPosition.Security.Symbol))//no esta siendo procesada por ningún otra instrucción
                        {
                            AccountPosition portfPos = onlinePositions.Where(x => x.Security.Symbol == instr.RelatedInstruction.AccountPosition.Security.Symbol).FirstOrDefault();
                            instr.Executed = true;
                            instr.Text = "No ejecutada por existencia de otras sincronizaciones";
                            instr.RelatedInstruction.IsOnlinePosition = portfPos.PositionStatus.IsOnline();
                            instr.RelatedInstruction.AccountPosition.PositionStatus = zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities.PositionStatus.GetUnwindSentToMarket();
                            ProcessUnwindPosition(instr.RelatedInstruction, portfPos);
                            InstructionManager.Persist(instr);
                            InstructionManager.Persist(instr.RelatedInstruction);

                        }
                        else
                        {//Se intenta descargar una posición que no existe
                            instr.Executed = true;
                            instr.Text = "No ejecutada por existencia de otras sincronizaciones";
                            Instruction relInstr = instr.RelatedInstruction;
                            InstructionManager.Persist(instr);

                            if (onlinePositions!=null && !onlinePositions.Any(x => x.Security.Symbol == instr.RelatedInstruction.AccountPosition.Security.Symbol))
                                MarkAsAlreadyUnwinded(ref relInstr);
                            else if (PositionInstructions.Keys.Any(x => x == instr.RelatedInstruction.AccountPosition.Security.Symbol))
                                MarkAsUnwindingInProgress(ref relInstr);
                        }
                    }
                    else
                    {
                        DoLog(string.Format("@{2} - Could not run related instruction for type {1} in Account {0} ",
                             IBRConfiguration.AccountNumber, instr.RelatedInstruction.InstructionType.Type, IBRConfiguration.Name),
                             Main.Common.Util.Constants.MessageType.Information);
                    }
                }
                else // Tenemos una instrx con alta/baja de posiciones masiva
                {
                    List<Instruction> newPosRelated = InstructionManager.GetRelatedInstructions(instr.Account.AccountNumber, instr.Id, InstructionType.GetNewPosInstr());
                    foreach (Instruction newPosInstr in newPosRelated)
                    {
                        AccountPosition prevPos = PositionManager.GetActivePositionBySymbol(newPosInstr.Symbol,newPosInstr.Account.Id);
                        if (prevPos != null)
                        {
                            newPosInstr.IsOnlinePosition = prevPos.PositionStatus.IsOnline();
                            newPosInstr.AccountPosition = prevPos;
                            //newPosInstr.Text = "Ejecutada sin sincronización";
                            ProcessNewPosition(newPosInstr);
                        }
                        else
                        {
                            newPosInstr.Executed = true;
                            newPosInstr.Text = "No ejecutada no encontrase posición sobre la cual operar";
                            InstructionManager.Persist(newPosInstr);
                        
                        }
                    }

                    instr.Executed = true;
                    instr.Text = "No ejecutada por existencia de otras sincronizaciones";
                    InstructionManager.Persist(instr);
                }
            }
        
        
        }

        protected void CancelRelatedInstructions(Instruction instr)
        {
            if (instr.RelatedInstruction != null)
            {
                instr.RelatedInstruction.Executed = true;
                instr.RelatedInstruction.Text = "No ejecutada por error de sincronización";
                InstructionManager.Persist(instr.RelatedInstruction);
            }
            else
            {
                List<Instruction> newPosRelated = InstructionManager.GetRelatedInstructions(instr.Account.AccountNumber, instr.Id, InstructionType.GetNewPosInstr());
                List<Instruction> unwindRelated = InstructionManager.GetRelatedInstructions(instr.Account.AccountNumber, instr.Id, InstructionType.GetUnwindPos());

                foreach (Instruction newPosInstr in newPosRelated)
                {
                    newPosInstr.Executed = true;
                    newPosInstr.Text = "No ejecutada por error de sincronización";
                    InstructionManager.Persist(newPosInstr);
                }

                foreach (Instruction unwindInstr in unwindRelated)
                {
                    unwindInstr.Executed = true;
                    unwindInstr.Text = "No ejecutada por error de sincronización";
                    InstructionManager.Persist(unwindInstr);
                }
            }
        }

        protected List<AccountPosition> GetCurrentPositionsInMarket(Account account)
        {
            AccountReferenceHandler.SyncAccountPositions(account);


            while (AccountReferenceHandler.WaitingAccountPositions() && !AccountReferenceHandler.IsAbortOnTimeout())
                Thread.Sleep(100);

            bool reqAccountPosition = AccountReferenceHandler.WaitingAccountPositions();
            bool abortOnTimeout = AccountReferenceHandler.IsAbortOnTimeout();

            if (!reqAccountPosition && !abortOnTimeout)
            {
                List<AccountPosition> onlinePositions = AccountReferenceHandler.GetActivePositions();

                return onlinePositions;

            }
            else
                return new List<AccountPosition>();
        }

        protected void ProcessAccounPositionsSync(List<Instruction> instructionsPositionsSync)
        {
            foreach (Instruction instr in instructionsPositionsSync)
            {
                try
                {
                    if (instr.Account.AccountNumber == IBRConfiguration.AccountNumber)
                    {
                        Account account = AccountManager.GetById(instr.Account.Id);
                        DoLog(string.Format("@{1} - Starting sync for account {0} ...", instr.Account.AccountNumber, IBRConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                        AccountReferenceHandler.SyncAccountPositions(account);

                        while (AccountReferenceHandler.WaitingAccountPositions() && !AccountReferenceHandler.IsAbortOnTimeout())
                            Thread.Sleep(100);

                        bool reqAccountPosition = AccountReferenceHandler.WaitingAccountPositions();
                        bool abortOnTimeout = AccountReferenceHandler.IsAbortOnTimeout();

                        if (!reqAccountPosition && !abortOnTimeout)
                        {
                            Instruction relInstr = null;
                            List<AccountPosition> onlinePositions = AccountReferenceHandler.GetActivePositions();
                            //we only consider available positions
                            PersistSyncPositions(instr, ref relInstr, onlinePositions);
                            DoLog(string.Format("@{1} - Account {0} positions succesfully synchronized", AccountReferenceHandler.GetAccountToSync().BrokerAccountName, IBRConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);

                            if (relInstr != null)
                            {
                                ProcessNewPositionSync(ref relInstr, onlinePositions,false);
                            }
                            else
                            {
                                List<Instruction> newPosRelated = InstructionManager.GetRelatedInstructions(account.AccountNumber,instr.Id, InstructionType.GetNewPosInstr());
                                foreach(Instruction newPosInstr in newPosRelated)
                                {
                                    Instruction newPosInstrCopy = newPosInstr;
                                    ProcessNewPositionSync(ref newPosInstrCopy, onlinePositions,false);
                                }

                                List<Instruction> unwindRelated = InstructionManager.GetRelatedInstructions(account.AccountNumber, instr.Id, InstructionType.GetUnwindPos());
                                foreach (Instruction unwindInstr in unwindRelated)
                                {
                                    Instruction unwindInstrCopy = unwindInstr;
                                    ProcessNewPositionSync(ref unwindInstrCopy, onlinePositions,true);
                                }
                            }
                        }
                        else
                        {
                            if (abortOnTimeout)
                                instr.Text = "Se produjo un timeout sincronizando la cuenta";
                            else if (reqAccountPosition)
                                instr.Text = "Se produjo un error sincronizando la cuenta";
                            else
                                instr.Text = "Error desconocido sicronizando la cuenta";

                            CancelRelatedInstructions(instr);
                        }

                        instr.Executed = true;
                        InstructionManager.Persist(instr);
                    }
                    else
                        DoLog(string.Format("@{1} - Discarding positions sync for account number: {0}", 
                                             instr.Account.AccountNumber, IBRConfiguration.Name),
                                             Main.Common.Util.Constants.MessageType.Information);

                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{2} - Critical error processing position sync instruction: {0} - {1}", 
                           ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", IBRConfiguration.Name), 
                           Main.Common.Util.Constants.MessageType.Error);
                }
            }
        }

        protected bool CanSyncAccount()
        {
            //We can only make a sync if there are no pending positions
            return PositionInstructions.Keys.Count == 0;
        
        }

        protected void DoFindInstructions()
        {
            bool run=true;
            while (run)
            {
                Thread.Sleep(IBRConfiguration.RoutingUpdateInMilliseconds);

                lock (tLock)
                {

                    try
                    {
                        List<Instruction> instructionsToProcess = InstructionManager.GetPendingInstructions(IBRConfiguration.AccountNumber);

                        if (CanSyncAccount())
                        {
                            //We process the account sync instructions
                            ProcessAccounBalanceSync(instructionsToProcess.Where(x => x.InstructionType.Type == InstructionType._SYNC_BALANCE).ToList());

                            //If there is nothing to sync, we check for post syncs
                            if (   instructionsToProcess.Where(x => x.InstructionType.Type == InstructionType._SYNC_POSITIONS).Count() == 0
                                && instructionsToProcess.Where(x => x.InstructionType.Type == InstructionType._POST_SYNC_POSITIONS).Count() > 0)
                                ProcessAccounPositionsSync(instructionsToProcess.Where(x => x.InstructionType.Type == InstructionType._POST_SYNC_POSITIONS).ToList());
                            

                            //We process the account positions sync instructions
                            ProcessAccounPositionsSync(instructionsToProcess.Where(x => x.InstructionType.Type == InstructionType._SYNC_POSITIONS).ToList());
                        }
                        else
                        {
                            //We process the sync positions without a sync
                            ProcessNewPositionsNoSync(instructionsToProcess.Where(x => x.InstructionType.Type == InstructionType._SYNC_POSITIONS).ToList());
                        }

                        //We process the cancelation of a specific position
                        ProcessAccountPositionCancel(instructionsToProcess.Where(x => x.InstructionType.Type == InstructionType._CANCEL_POSITIONS).ToList());

                        //We process the cleaning of all opened positions
                        ProcessPositionsCleaningSync(instructionsToProcess.Where(x => x.InstructionType.Type == InstructionType._CLEAN_ALL_POS).ToList());

                    }
                    catch (Exception ex)
                    {
                        run = false;//There is no reason to go on as there is some issues in the DB
                        DoLog(string.Format("@{2} - Critical error processing instructions: {0} - {1}",
                                            ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", IBRConfiguration.Name),
                                            Main.Common.Util.Constants.MessageType.Error);
                    }
                }
            }
        }

        private void ProcessAccountPositionCancel(List<Instruction> cancelInstrxs)
        {
            foreach (Instruction cancelInstrx in cancelInstrxs)
            {
                if (cancelInstrx.Account.AccountNumber == IBRConfiguration.AccountNumber)
                {
                    try
                    {
                        ExecutionSummary summary = ExecutionSummaries.Values.Where(x => x.Symbol == cancelInstrx.Symbol).FirstOrDefault();

                        if (summary != null)
                        {
                            CancelPositionWrapper cancelPositionWrapper = new CancelPositionWrapper(summary.Position, IBRConfiguration);
                            OrderRouter.ProcessMessage(cancelPositionWrapper);
                            cancelInstrx.Executed = true;
                            InstructionManager.Persist(cancelInstrx);
                        }
                        else
                        {
                            cancelInstrx.Executed = true;
                            cancelInstrx.Text = string.Format("Descartada porque no se encontró una posición abierta para el código de especie {0}", cancelInstrx.Symbol);
                            InstructionManager.Persist(cancelInstrx);
                        }

                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("@{2} - Critical error processing cancel instruction: {0} - {1}",
                               ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", IBRConfiguration.Name),
                               Main.Common.Util.Constants.MessageType.Error);
                    }
                
                }
                else
                    DoLog(string.Format("@{1} - Discarding cancel instructions account number: {0}",
                                         cancelInstrx.Account.AccountNumber, IBRConfiguration.Name),
                                         Main.Common.Util.Constants.MessageType.Information);
            
            }
        }

        private List<AccountPosition> GetPositionsToPersist(string relSymbol, List<AccountPosition> positions)
        {
            List<AccountPosition> positionsToPersist = new List<AccountPosition>();

            List<Instruction> instructionsToProcess = InstructionManager.GetPendingInstructions(IBRConfiguration.AccountNumber);

            foreach (AccountPosition pos in positions)
            {
                bool found = false;

                foreach (Instruction instrx in instructionsToProcess.Where(x => x.InstructionType.Type == InstructionType._SYNC_POSITIONS))
                {
                    if (!instrx.Executed && instrx.RelatedInstruction != null)
                    {
                        if (instrx.RelatedInstruction.Symbol == pos.Security.Symbol)
                            found = true;
                    }
                
                }


                if (!found  && relSymbol != pos.Security.Symbol)
                    positionsToPersist.Add(pos);
            
            }

            return positionsToPersist;
        
        }

        private void PersistSyncPositions(Instruction instr, ref Instruction relInstr, List<AccountPosition> positions)
        {
            if (instr.RelatedInstruction != null)//Tenemos una instrx individual
            {
                relInstr = instr.RelatedInstruction;
                string relSymbol = relInstr.Symbol;
                //PositionManager.PersistAndReplace(positions.Where(x => x.Security.Symbol != relSymbol ).ToList(),instr.Account.Id);

                PositionManager.PersistAndReplace(GetPositionsToPersist(relSymbol, positions), instr.Account.Id);
            }
            else // Tenemos una instrx con alta/baja de posiciones masiva
            {
                List<Instruction> newPosRelated = InstructionManager.GetRelatedInstructions(instr.Account.AccountNumber, instr.Id, InstructionType.GetNewPosInstr());
                List<Instruction> unwindPossRelated = InstructionManager.GetRelatedInstructions(instr.Account.AccountNumber, instr.Id, InstructionType.GetUnwindPos());
                
                List<AccountPosition> positionsToPersist = new List<AccountPosition>();

                foreach (AccountPosition pos in positions)//Solo actualizamos las posiciones no afectadas por la instrx
                {
                    if (!newPosRelated.Any(x => x.Symbol == pos.Security.Symbol)
                        && !unwindPossRelated.Any(x=>x.Symbol==pos.Security.Symbol))
                    {
                        if (pos.Account.AccountNumber == instr.Account.AccountNumber)
                            positionsToPersist.Add(pos);
                    }
                }

                PositionManager.PersistAndReplace(positionsToPersist, instr.Account.Id);
            }
        }

        protected virtual void ProcessNewPosition(Instruction instr)
        {

            DoLog(string.Format("{0}: Creating position for symbol {1}",IBRConfiguration.Name ,instr.Symbol), Constants.MessageType.Information);

            Position pos = new Position()
            {
                Security = new Security()
                                        {
                                            Symbol = instr.Symbol,
                                            MarketData = null,
                                            Currency = instr.Account.Currency,
                                            SecType = instr.SecurityType
                                        },
                Side = zHFT.Main.Common.Enums.Side.Buy,//A new positions is always a buy positions
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = instr.Account != null ? instr.Account.GenericAccountNumber : null
            };

            if (instr.Shares.HasValue)
            {
                pos.Qty = Convert.ToDouble(instr.Shares);
                pos.QuantityType = QuantityType.SHARES;
            }
            else if (instr.Ammount.HasValue)
            {
                pos.CashQty = Convert.ToDouble(instr.Ammount);
                pos.QuantityType = QuantityType.CURRENCY;
            }
            else
                DoLog(string.Format("{0}: Discarding new position because it was not specified a number of shares or an ammount. Symbol = {1}", IBRConfiguration.Name, instr.Symbol), Constants.MessageType.Information);


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

            MarketDataRequestWrapper mdReqWrapper = new MarketDataRequestWrapper(pos.Security,
                                                                                 SubscriptionRequestType.SnapshotAndUpdates,
                                                                                 instr.QuoteSymbol);


            OnMessageRcv(mdReqWrapper);
        }

        protected virtual bool EvalMarketData(ExecutionSummary summary)
        {
            if (summary.Position.Security.MarketData != null && summary.Position.Security.MarketData.Trade.HasValue)
            {

                if (summary.Position.QuantityType == QuantityType.CURRENCY
                    && summary.Position.Security.MarketData.Trade > summary.Position.CashQty.Value)
                {
                    Instruction instr = PositionInstructions[summary.Position.Symbol];
                    if (instr != null)
                    {
                        MarkAsNotEnoughMoney(ref instr);
                        PositionInstructions.Remove(summary.Position.Symbol);
                        ExecutionSummaries.Remove(summary.Position.Symbol);
                        Positions.Remove(summary.Position.Symbol);
                        return false;
                    }
                    else
                        return false;
                }
                else
                    return true;
            }
            else
            {
                return false;
            }
        }

        protected  void MarkAsUnwindRejected(ref Instruction relInstr,string desc)
        {
            relInstr.Executed = true;
            relInstr.Text = desc;

            if (relInstr.AccountPosition != null && relInstr.AccountPosition.PositionStatus != null)
                relInstr.AccountPosition.PositionStatus = InstructionBasedRouting.BusinessEntities.PositionStatus.GetNewPositionStatus(true);

            InstructionManager.Persist(relInstr);

            DoLog(string.Format("@{0} - {1}",IBRConfiguration.Name,desc), Main.Common.Util.Constants.MessageType.Error);

        }

        protected virtual void ProcessUnwindPosition(Instruction instr, AccountPosition portfPos)
        {

            DoLog(string.Format("{0}: Unwinding position for symbol {1}", IBRConfiguration.Name, instr.Symbol), Constants.MessageType.Information);

            zHFT.Main.Common.Enums.Side unwdSide = zHFT.Main.Common.Enums.Side.Sell;
            if (portfPos != null && portfPos.Shares.HasValue)
                unwdSide = portfPos.Shares > 0 ? zHFT.Main.Common.Enums.Side.Sell : zHFT.Main.Common.Enums.Side.Buy;
            else if (portfPos != null && portfPos.Ammount.HasValue)
                unwdSide = portfPos.Ammount > 0 ? zHFT.Main.Common.Enums.Side.Sell : zHFT.Main.Common.Enums.Side.Buy;
            else
            {
                MarkAsUnwindRejected(ref instr, string.Format("Critical ERROR: Could not unwind a position for symbol {0} when there is not that position in the exchange!", instr.Symbol));
                return;
            }   

            Position pos = new Position()
            {
                Security = new Security() 
                                        { 
                                            Symbol = instr.Symbol, 
                                            MarketData = null,
                                            Currency = instr.Account.Currency,
                                            SecType = instr.SecurityType
                                        },
                Side = unwdSide,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = instr.Account != null ? instr.Account.GenericAccountNumber : null
            };

            if (instr.Shares.HasValue)
            {
                pos.Qty = Convert.ToDouble(Math.Abs(instr.Shares.Value));
                pos.QuantityType = QuantityType.SHARES;
            }
            else if (instr.Ammount.HasValue)
            {
                pos.Qty = Convert.ToDouble(Math.Abs(instr.Ammount.Value));
                pos.QuantityType = QuantityType.OTHER;
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

            MarketDataRequestWrapper mdReqWrapper = new MarketDataRequestWrapper(pos.Security,
                                                                                 SubscriptionRequestType.SnapshotAndUpdates,
                                                                                 instr.QuoteSymbol);


            OnMessageRcv(mdReqWrapper);
        }

        #endregion

        #region Protected Overriden Methods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        protected void CleanPrevInstructions()
        {
            List<Instruction> prevInstrx =  InstructionManager.GetPendingInstructions(IBRConfiguration.AccountNumber);
            
            foreach(Instruction prevInstr in prevInstrx)
            {
                prevInstr.Executed=true;
                prevInstr.AccountPosition = null;
                InstructionManager.Persist(prevInstr);
            }
        
        }

        protected override bool OnInitialize()
        {
            try
            {
                ExecutionSummaries = new Dictionary<string, ExecutionSummary>();
                PositionInstructions = new Dictionary<string, Instruction>();
                
                var accountManagerAccessLayer = Type.GetType(IBRConfiguration.AccountManagerAccessLayer);
                if (accountManagerAccessLayer != null)
                    AccountManager = (IAccountManagerAccessLayer)Activator.CreateInstance(accountManagerAccessLayer, IBRConfiguration.ADOInstructionsAccessLayerConnectionString);
                else
                {
                    DoLog("assembly not found: " + IBRConfiguration.AccountManagerAccessLayer, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }

                var positionManagerAccessLayer = Type.GetType(IBRConfiguration.PositionManagerAccessLayer);
                if (positionManagerAccessLayer != null)
                    PositionManager = (IPositionManagerAccessLayer)Activator.CreateInstance(positionManagerAccessLayer, IBRConfiguration.ADOInstructionsAccessLayerConnectionString);
                else
                {
                    DoLog("assembly not found: " + IBRConfiguration.PositionManagerAccessLayer, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }

                var instructionManagerAccessLayer = Type.GetType(IBRConfiguration.InstructionManagerAccessLayer);
                if (accountManagerAccessLayer != null)
                    InstructionManager = (IInstructionManagerAccessLayer)Activator.CreateInstance(instructionManagerAccessLayer, IBRConfiguration.ADOInstructionsAccessLayerConnectionString, AccountManager);
                else
                {
                    DoLog("assembly not found: " + IBRConfiguration.InstructionManagerAccessLayer, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }


                var accountReferenceHandler = Type.GetType(IBRConfiguration.AccoutReferenceHandler);
                if (accountReferenceHandler != null)
                    AccountReferenceHandler = (IAccountReferenceHandler)Activator.CreateInstance(accountReferenceHandler, OnLogMsg, IBRConfiguration.AccountManagerConfig);
                else
                {
                    DoLog("assembly not found: " + IBRConfiguration.AccoutReferenceHandler, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }

                CleanPrevInstructions();

                ProcessInstructionsThread = new Thread(DoFindInstructions);
                ProcessInstructionsThread.Start();

                return true;
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + IBRConfiguration.Name + ":" + ex.Message + "-" + (ex.StackTrace!=null?ex.StackTrace:""), Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        protected void RunCancelPositionOnOrderFailed(ExecutionSummary summary)
        {
            summary.Position.PositionCanceledOrRejected = true;
            summary.Position.PositionCleared = false;
            summary.Position.SetPositionStatusFromExecution(ExecType.Canceled);
            Instruction instr = PositionInstructions[summary.Position.Symbol];

            if (instr != null)
            {
                ProcessInstructionExecuted(instr, summary);
                SaveExecutionSummary(summary,(int?)IBRConfiguration.AccountNumber);
                PositionInstructions.Remove(summary.Position.Symbol);
                ExecutionSummaries.Remove(summary.Position.Symbol);
            }
        }

        protected override CMState ProcessMarketData(Main.Common.Wrappers.Wrapper wrapper)
        {
            lock (tLock)
            {
                string symbol = (string)wrapper.GetField(MarketDataFields.Symbol);

                if (ExecutionSummaries.Keys.Contains(symbol))
                {
                    ExecutionSummary summary = ExecutionSummaries[symbol];

                    if (!EvalPositionOpened(symbol))//Position is not opened
                    {
                        summary.Position.Security.MarketData = MarketDataConverter.GetMarketData(wrapper, Config);
                        if (EvalMarketData(summary))
                        {
                            CMState result = OpenPositionOnMarket(summary);
                            if (result.Success)
                                return result;
                            else
                            {
                                RunCancelPositionOnOrderFailed(summary);
                            }
                        }
                        else
                            return CMState.BuildSuccess();
                    }
                    else
                    {

                        if (!EvalPositionCleared(symbol) && !EvalPositionCanceledOrRejected(symbol))
                        {
                            CMState state = OrderRouter.ProcessMessage(wrapper);

                            if (!state.Success)
                                DoLog(string.Format("{0}: Could not process market data for symbol {1}", IBRConfiguration.Name, symbol), Constants.MessageType.Information);

                            return state;
                        }
                        else
                            return CMState.BuildSuccess();
                    }

                }

                return CMState.BuildSuccess();
            }
        }

        protected void  ProcessInstructionExecuted(Instruction instr,ExecutionSummary summary)
        {
            if (instr.InstructionType.Type == InstructionType._NEW_POSITION)
            {
                instr.Executed = true;
                bool online = true;

                if (summary.Position.PositionCleared)
                {
                    online = true;
                    instr.Text = summary.Text;

                    if (instr.IsMerge)//ya estaba online, actualzamos las shares
                        instr.AccountPosition.Shares += Convert.ToInt32(summary.CumQty);
                    else
                        instr.AccountPosition.Shares = Convert.ToInt32(summary.CumQty);
                    
                }
                else if (summary.Position.PositionCanceledOrRejected)
                {
                    online = instr.IsOnlinePosition;
                    instr.Text = summary.Text;
                }
                else
                    throw new Exception(string.Format("New Position: Invalid state for position for symbol {0} @OnEvalExecutionSummary", summary.Position.Symbol));

                instr.AccountPosition.PositionStatus = InstructionBasedRouting.BusinessEntities.PositionStatus.GetNewPositionStatus(online);
                InstructionManager.Persist(instr);
            }
            else if (instr.InstructionType.Type == InstructionType._UNWIND_POSITION)
            {
                instr.Executed = true;

                if (summary.Position.PositionCleared)
                {
                    if (instr.Shares == instr.AccountPosition.Shares || instr.IsFromUnwindAll)
                    {
                        instr.AccountPosition.PositionStatus = InstructionBasedRouting.BusinessEntities.PositionStatus.GetOfflineUnwindedStatus();
                        instr.AccountPosition.Active = false;
                    }
                    else
                    {
                        instr.AccountPosition.PositionStatus = InstructionBasedRouting.BusinessEntities.PositionStatus.GetNewPositionStatus(true);
                        instr.AccountPosition.Shares -= instr.Shares;
                        instr.AccountPosition.Active = true;
                    }

                }
                else if (summary.Position.PositionCanceledOrRejected)
                    instr.AccountPosition.PositionStatus = InstructionBasedRouting.BusinessEntities.PositionStatus.GetNewPositionStatus(true);//Si hubiera estado offline se hubiera descartado la instrx
                else
                    throw new Exception(string.Format("Unwinding Position: Invalid state for position for symbol {0} @OnEvalExecutionSummary", summary.Position.Symbol));

                InstructionManager.Persist(instr);
            }
        }

        protected override void UnsuscribeMarketData(Position pos)
        {
            MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(pos.Security, SubscriptionRequestType.Unsuscribe);
            OnMessageRcv(wrapper);
        
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
                        if (summary.IsFilledPosition()  || summary.Position.PositionCanceledOrRejected)
                        {
                            summary.Position.PositionCleared = summary.LeavesQty <= 0;

                            if (PositionInstructions.ContainsKey(summary.Position.Symbol))
                            {
                                Instruction instr = PositionInstructions[summary.Position.Symbol];

                                if (instr == null)
                                    throw new Exception(string.Format("@OnEvalExecutionSummary Could not find an instruction for symbol {0}", summary.Position.Symbol));

                                ProcessInstructionExecuted(instr, summary);
                                SaveExecutionSummary(summary, (int?)IBRConfiguration.AccountNumber);
                                PositionInstructions.Remove(summary.Position.Symbol);
                                ExecutionSummaries.Remove(summary.Position.Symbol);
                                Positions.Remove(summary.Position.Symbol);
                                UnsuscribeMarketData(summary.Position);
                            }
                            else
                            {
                                DoLog(string.Format("@{0} OnEvalExecutionSummary: Position already processed: {1}.Filled={2},CancelOrRejecter={3}",
                                                            IBRConfiguration.Name, summary.Position.Symbol, 
                                                            summary.IsFilledPosition(),
                                                            summary.Position.PositionCanceledOrRejected), 
                                                            Constants.MessageType.Information);
                            
                            }
                        }
                        else
                            SaveExecutionSummary(summary, (int?)IBRConfiguration.AccountNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0} OnEvalExecutionSummary: Critical error processing execution summary: {1}", IBRConfiguration.Name, ex.Message), Constants.MessageType.Error);
                CancelAllNotCleared((int?)IBRConfiguration.AccountNumber);
            }
        }

        protected override void OnProcessNewPositionCanceled(Main.BusinessEntities.Positions.Position pos)
        {
            DoLog(string.Format("@{0} OnProcessNewPositionCanceled not implemented", IBRConfiguration.Name), Constants.MessageType.Error);
        }

        #endregion
    }
}
