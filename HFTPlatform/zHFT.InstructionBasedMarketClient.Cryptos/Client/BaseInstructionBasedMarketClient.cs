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

        protected Dictionary<long, Security> ActiveSecurities { get; set; }

        protected Dictionary<long, DateTime> ContractsTimeStamps { get; set; }

        protected Thread ProcessInstructionsThread { get; set; }

        protected Thread RequestMarketDataThread { get; set; }

        protected Thread CleanOldSecuritiesThread { get; set; }

        protected AccountManager AccountManager { get; set; }

        #endregion

        #region Abstract Methods

        protected abstract void DoRequestOrderBook(Object param);

        protected abstract void DoRequestMarketData(Object param);

        protected abstract CMState ProcessMarketDataRequest(Wrapper wrapper);

        protected abstract int GetSearchForInstrInMiliseconds();

        protected abstract BaseConfiguration GetConfig();

        protected abstract int GetAccountNumber();

        #endregion

        #region Protected Methods

        protected void RemoveSymbol(string symbol)
        {
            List<long> keysToRemove = new List<long>();

            foreach (long key in ActiveSecurities.Keys)
            {
                Security sec = ActiveSecurities[key];

                if (sec.Symbol == symbol)
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (long keyToRemove in keysToRemove)
            {
                if(ContractsTimeStamps.ContainsKey(keyToRemove))
                    ContractsTimeStamps.Remove(keyToRemove);
                
                if(ActiveSecurities.ContainsKey(keyToRemove))
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
                        List<long> keysToRemove = new List<long>();
                        foreach (long key in ContractsTimeStamps.Keys)
                        {
                            DateTime timeStamp = ContractsTimeStamps[key];

                            if ((DateTime.Now - timeStamp).Hours >= _MAX_ELAPSED_HOURS_FOR_MARKET_DATA)
                            {
                                keysToRemove.Add(key);
                            }
                        }

                        foreach (long keyToRemove in keysToRemove)
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

        protected CMState OnMarketDataRequest(Wrapper wrapper)
        {
            string symbol = (string)wrapper.GetField(MarketDataRequestField.Symbol);
            long mdReqId = (long)wrapper.GetField(MarketDataRequestField.MDReqId);
            string quoteSymbol = (string)wrapper.GetField(MarketDataRequestField.QuoteSymbol);

            Security sec = new Security() { Symbol = symbol };

            ActiveSecurities.Add(mdReqId, sec);
            RequestMarketDataThread = new Thread(DoRequestMarketData);
            RequestMarketDataThread.Start(new object[] { symbol ,quoteSymbol});

            return CMState.BuildSuccess();
        }
        
        protected CMState OnOrderBookRequest(Wrapper wrapper)
        {
            string symbol = (string)wrapper.GetField(MarketDataRequestField.Symbol);
            long mdReqId = (long)wrapper.GetField(MarketDataRequestField.MDReqId);
            string quoteSymbol = (string)wrapper.GetField(MarketDataRequestField.QuoteSymbol);

            Security sec = new Security() { Symbol = symbol };

            ActiveSecurities.Add(mdReqId, sec);
            RequestMarketDataThread = new Thread(DoRequestOrderBook);
            RequestMarketDataThread.Start(new object[] { symbol ,quoteSymbol});

            return CMState.BuildSuccess();
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
                        return ProcessMarketDataRequest(wrapper);
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
