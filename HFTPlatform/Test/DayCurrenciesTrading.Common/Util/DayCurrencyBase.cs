using System;
using System.Collections.Generic;
using System.Threading;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Wrappers;

namespace DayCurrenciesTrading.Common.Util
{
    public class DayCurrencyBase
    {
        #region Public Attributes
        
        protected OnLogMessage OnLogMsg { get; set; }

        protected OnMessageReceived OnMessageRcv { get; set; }

        protected IConfiguration Config { get; set; }
        
        protected object tLock { get; set; }
        
        protected Dictionary<string, Position> PendingCancels { get; set; }
        
        protected ICommunicationModule MarketClientModule { get; set; }
        
        protected ICommunicationModule OrderRouter { get; set; }
        
        protected int NextPosId { get; set; }
        
        #endregion
        
        #region Protected Methods
        
        protected void PendingCancelTimeoutThread(object symbol)
        {
            try
            {
                Thread.Sleep(60 * 1000);
                if(PendingCancels.ContainsKey(symbol.ToString()))
                    PendingCancels.Remove(symbol.ToString());
            }
            catch (Exception ex)
            {
                DoLog(string.Format("{0} Critical Error removing Pending Cancel for Symbol {1}={2}",
                    "PendilCancelTimeoutThread", symbol.ToString(), ex.Message),Constants.MessageType.Error);
            }
        }
        
        protected CMState CancelRoutingPos( Position rPos)
        {
            if (!PendingCancels.ContainsKey(rPos.Symbol))
            {
                lock (PendingCancels)
                {
                    //We have to cancel the position before closing it.
                    PendingCancels.Add(rPos.Symbol, rPos);
                    new Thread(PendingCancelTimeoutThread).Start(rPos.Symbol);
                }
                CancelPositionWrapper cancelWrapper = new CancelPositionWrapper(rPos, Config);
                return OrderRouter.ProcessMessage(cancelWrapper);
            }
            else
                return CMState.BuildSuccess();
        }
        
        private Position LoadCloseRegularPos(Position openPos)
        {
            Position pos = new Position()
            {
                Security = openPos.Security,
                Side = openPos.Side == Side.Buy ? Side.Sell : Side.Buy,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                Qty = openPos.CumQty,
                QuantityType = QuantityType.SHARES,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
            };


            pos.PositionCleared = true;
            pos.LoadPosId(NextPosId);
            NextPosId++;

            return pos;
        }
        
        protected Position RunClose(Position openPos)
        {
            if (openPos.PosStatus == PositionStatus.Filled)
            {
                Position routingClosingPos = LoadCloseRegularPos(openPos);
                
                PositionWrapper posWrapper = new PositionWrapper(routingClosingPos, Config);
                CMState state= OrderRouter.ProcessMessage(posWrapper);

                if (!state.Success)
                    throw state.Exception;

                return routingClosingPos;

            }
            else if (openPos.PositionRouting())
            {
                DoLog(string.Format("Canceling routing pos for symbol {0} before closing (status={1} posId={2})",openPos.Security.Symbol,openPos.PosStatus,openPos.PosId),Constants.MessageType.Information);
                CMState state=  CancelRoutingPos(openPos);
                
                if (!state.Success)
                    throw state.Exception;
                return null;
            }
            else
            {
                DoLog(string.Format("{0} Aborting  position on invalid state. Symbol {1} Qty={2} PosStatus={3} PosId={4}", 
                        openPos.Side, openPos.Security.Symbol, openPos.Qty,openPos.PosStatus.ToString(),openPos.PosId), 
                    Constants.MessageType.Information);
                return null;
            }
        }
        
        
        public void DoLog(string msg, Constants.MessageType type)
        {
            if(OnLogMsg!=null)
                OnLogMsg(string.Format("{0}", msg), type);
        }

        public void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Configuration.Configuration().GetConfiguration<Configuration.Configuration>(configFile, noValueFields);
        }


        protected CMState ProcessMarketData(Wrapper wrapper)
        {
            //TODO build
            return null;
        }

        #endregion
        
        #region Protected Methods

        protected int GetNextMDReqId()
        {
            Thread.Sleep(10);
            return Convert.ToInt32(DateTime.Now.Millisecond);
            
        }

        protected ICommunicationModule LoadModule(string assembly, string assemblyName)
        {
            DoLog("Initializing Order Router " + assembly, Constants.MessageType.Information);
            ICommunicationModule module = null;
            if (!string.IsNullOrEmpty(assembly))
            {
                var type = Type.GetType(assembly);
                if (type != null)
                {
                    module = (ICommunicationModule)Activator.CreateInstance(type);
                    

                }
                else
                    throw new Exception("assembly not found: " + assembly);
            }
            else
                DoLog(string.Format("{0} not found. It will not be initialized",assemblyName), Constants.MessageType.Error);

            return module;
        }

        #endregion
    }
}