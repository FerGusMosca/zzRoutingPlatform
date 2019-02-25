using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.InstructionBasedMarketClient.Cryptos.DataAccessLayer.Managers;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.InstructionBasedMarketClient.Cryptos.Client
{
    public abstract class BaseInstructionBasedMarketClient : BaseCommunicationModule
    {

        #region protected  Consts

        protected int _SECURITIES_REMOVEL_PERIOD = 60 * 60 * 1000;//Once every hour in milliseconds

        protected int _MAX_ELAPSED_HOURS_FOR_MARKET_DATA = 12;

        #endregion

        #region Protected Attributes

        protected Dictionary<int, Security> ActiveSecurities { get; set; }

        protected Dictionary<int, DateTime> ContractsTimeStamps { get; set; }

        protected Thread ProcessInstructionsThread { get; set; }

        protected Thread RequestMarketDataThread { get; set; }

        protected Thread CleanOldSecuritiesThread { get; set; }

        protected InstructionManager InstructionManager { get; set; }

        protected AccountManager AccountManager { get; set; }

        #endregion

        #region Abstract Methods

        protected abstract void DoRequestMarketData(Object param);

        protected abstract CMState ProessMarketDataRequest(Wrapper wrapper);

        protected abstract int GetSearchForInstrInMiliseconds();

        protected abstract BaseConfiguration GetConfig();

        protected abstract int GetAccountNumber();

        #endregion

        #region Protected Methods

        protected void CleanPrevInstructions()
        {
            List<Instruction> prevInstrx = InstructionManager.GetPendingInstructions(GetAccountNumber());

            foreach (Instruction prevInstr in prevInstrx)
            {
                prevInstr.Executed = true;
                prevInstr.AccountPosition = null;
                InstructionManager.Persist(prevInstr);
            }
        }

        protected void RemoveSymbol(string symbol)
        {
            List<int> keysToRemove = new List<int>();

            foreach (int key in ActiveSecurities.Keys)
            {
                Security sec = ActiveSecurities[key];

                if (sec.Symbol == symbol)
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (int keyToRemove in keysToRemove)
            {
                ContractsTimeStamps.Remove(keyToRemove);
                ActiveSecurities.Remove(keyToRemove);
            }
        }

        protected void DoCleanOldSecurities()
        {
            while (true)
            {
                Thread.Sleep(_SECURITIES_REMOVEL_PERIOD);//Once every hour

                lock (tLock)
                {
                    try
                    {
                        List<int> keysToRemove = new List<int>();
                        foreach (int key in ContractsTimeStamps.Keys)
                        {
                            DateTime timeStamp = ContractsTimeStamps[key];

                            if ((DateTime.Now - timeStamp).Hours >= _MAX_ELAPSED_HOURS_FOR_MARKET_DATA)
                            {
                                keysToRemove.Add(key);
                            }
                        }

                        foreach (int keyToRemove in keysToRemove)
                        {
                            ContractsTimeStamps.Remove(keyToRemove);
                            ActiveSecurities.Remove(keyToRemove);
                        }
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("@{1}: There was an error cleaning old securities from market data flow error={0} ", ex.Message, GetConfig().Name),
                              Main.Common.Util.Constants.MessageType.Error);
                    }
                }
            }
        }

        protected Security BuildSecurityFromInstruction(Instruction instrx)
        {

            Security sec = new Security()
            {
                Symbol = instrx.Symbol,
                SecType = SecurityType.CC
            };

            return sec;
        }

        protected void ProcessPositionInstruction(Instruction instr)
        {
            try
            {
                if (instr != null)
                {
                    if (!ActiveSecurities.Keys.Contains(instr.Id)
                        && !ActiveSecurities.Values.Where(x => x.Active).Any(x => x.Symbol == instr.Symbol))
                    {
                        instr = InstructionManager.GetById(instr.Id);

                        if (instr.InstructionType.Type == InstructionType._NEW_POSITION || instr.InstructionType.Type == InstructionType._UNWIND_POSITION)
                        {
                            ActiveSecurities.Add(instr.Id, BuildSecurityFromInstruction(instr));
                            RequestMarketDataThread = new Thread(DoRequestMarketData);
                            RequestMarketDataThread.Start(instr.Symbol);
                        }
                    }
                }
                else
                    throw new Exception(string.Format("Could not find a related instruction for id {0}", instr.Id));


            }
            catch (Exception ex)
            {

                DoLog(string.Format("Critical error processing related instruction: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : "")), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected CMState ProcessMarketDataRequest(Wrapper wrapper)
        {
            string symbol = (string)wrapper.GetField(MarketDataRequestField.Symbol);
            int mdReqId = (int)wrapper.GetField(MarketDataRequestField.MDReqId);

            Security sec = new Security() { Symbol = symbol };

            ActiveSecurities.Add(mdReqId, sec);

           
            RequestMarketDataThread = new Thread(DoRequestMarketData);
            RequestMarketDataThread.Start(symbol);

            return CMState.BuildSuccess();
        }

        protected void DoFindInstructions()
        {
            while (true)
            {
                Thread.Sleep(GetSearchForInstrInMiliseconds());

                lock (tLock)
                {
                    List<Instruction> instructionsToProcess = InstructionManager.GetPendingInstructions(GetAccountNumber());

                    try
                    {
                        foreach (Instruction instr in instructionsToProcess.Where(x => x.InstructionType.Type == InstructionType._NEW_POSITION || x.InstructionType.Type == InstructionType._UNWIND_POSITION))
                        {
                            //We process the account positions sync instructions
                            ProcessPositionInstruction(instr);
                        }
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("@{2}:Critical error processing instructions: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""), GetConfig().Name), Main.Common.Util.Constants.MessageType.Error);
                    }
                }
            }
        }

        protected void CancelMarketData(Security sec)
        {
            if (ActiveSecurities.Values.Any(x => x.Symbol == sec.Symbol))
            {
                List<Security> toUnsubscribeList = ActiveSecurities.Values.Where(x => x.Symbol == sec.Symbol).ToList();
                foreach (Security toUnsuscribe in toUnsubscribeList)
                {
                    toUnsuscribe.Active = false;
                }
                DoLog(string.Format("@{0}:Requesting Unsubscribe Market Data On Demand for Symbol: {0}", GetConfig().Name, sec.Symbol), Main.Common.Util.Constants.MessageType.Information);
            }
            else
                throw new Exception(string.Format("@{0}: Could not find active security to unsubscribe for symbol {1}", GetConfig().Name, sec.Symbol));

        }

        protected virtual CMState ProcessSecurityListRequest(Wrapper wrapper)
        {

            return CMState.BuildSuccess();
        }

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    if (action == Actions.SECURITY_LIST_REQUEST)
                    {
                        return ProcessSecurityListRequest(wrapper);
                    }
                    else if (Actions.MARKET_DATA_REQUEST == action)
                    {
                        return ProessMarketDataRequest(wrapper);
                    }
                    else
                    {
                        DoLog("Sending message " + action + " not implemented", Main.Common.Util.Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception("Sending message " + action + " not implemented"));
                    }
                }
                else
                    throw new Exception("Invalid Wrapper");

            }
            catch (Exception ex)
            {
                DoLog(ex.Message, Main.Common.Util.Constants.MessageType.Error);
                throw;
            }
        }

        #endregion

    }
}
