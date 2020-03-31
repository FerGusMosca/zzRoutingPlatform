using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.InstrFullMarketConnectivity.IOL.Common;
using zHFT.InstrFullMarketConnectivity.IOL.DataAccessLayer;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.InstructionBasedMarketClient.DataAccessLayer.Managers;
using zHFT.InstructionBasedMarketClient.IOL.Common.Configuration;
using zHFT.InstructionBasedMarketClient.IOL.Common.Wrappers;
using zHFT.InstructionBasedMarketClient.IOL.DataAccessLayer;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common;
using zHFT.MarketClient.Common.Converters;

namespace zHFT.InstructionBasedMarketClient.IOL.Client
{
    public class IOLInstructionBasedMarketClient : MarketClientBase, ICommunicationModule
    {
        #region Private Attributes

        public IConfiguration Config { get; set; }

        protected Configuration IOLConfiguration
        {
            get { return (Configuration)Config; }
            set { Config = value; }
        }

        protected Thread ProcessInstructionsThread { get; set; }

        protected Thread CleanOldSecuritiesThread { get; set; }

        protected Thread RequestMarketDataThread { get; set; }

        private InstructionManager InstructionManager { get; set; }

        private Dictionary<int, Security> ActiveSecurities { get; set; }

        private Dictionary<int, Security> ActiveSecuritiesOnDemand { get; set; }

        private Dictionary<int, DateTime> ContractsTimeStamps { get; set; }

        private BaseManager IOLMarketDataManager { get; set; }

        protected object tLock = new object();

        #endregion

        #region Constructors

        public IOLInstructionBasedMarketClient() { }

        #endregion

        #region Private Methods

        private Security LoadMarketData(Security sec, SettlType settlType, zHFT.InstructionBasedMarketClient.IOL.Common.DTO.MarketData marketData)
        {
           
            sec.MarketData.Trade = marketData.ultimoPrecio;
            sec.MarketData.OpeningPrice = marketData.apertura;
            sec.MarketData.TradingSessionHighPrice = marketData.maximo;
            sec.MarketData.TradingSessionLowPrice = marketData.minimo;
            sec.MarketData.MDEntryDate = marketData.fechaHora;
            sec.MarketData.CashVolume = marketData.montoOperado;
            sec.MarketData.NominalVolume = marketData.volumenNominal;
            sec.MarketData.MidPrice = marketData.precioPromedio;
            sec.MarketData.Currency = marketData.moneda;
            sec.MarketData.OpenInterest = marketData.interesesAbiertos;
            sec.MarketData.TradeVolume = marketData.cantidadOperaciones;
            sec.MarketData.SettlType = settlType;

            if (marketData.puntas.OrderByDescending(x => x.precioCompra).FirstOrDefault() != null && marketData.puntas.OrderByDescending(x => x.precioCompra).FirstOrDefault().cantidadCompra != 0)
            {
                sec.MarketData.BestBidPrice = marketData.puntas.OrderByDescending(x => x.precioCompra).FirstOrDefault().precioCompra;
                sec.MarketData.BestBidSize = Convert.ToInt64(marketData.puntas.OrderByDescending(x => x.precioCompra).FirstOrDefault().cantidadCompra);
            }


            if (marketData.puntas.OrderBy(x => x.precioVenta).FirstOrDefault() != null && marketData.puntas.OrderBy(x => x.precioVenta).FirstOrDefault().cantidadVenta != 0)
            {
                sec.MarketData.BestAskPrice = marketData.puntas.OrderBy(x => x.precioVenta).FirstOrDefault().precioVenta;
                sec.MarketData.BestAskSize = Convert.ToInt64(marketData.puntas.OrderBy(x => x.precioVenta).FirstOrDefault().cantidadVenta);
            }
         
            return sec;
        }


        #endregion

        #region Protected Methods

        protected override IConfiguration GetConfig() { return Config; }

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Configuration().GetConfiguration<Configuration>(configFile, noValueFields);
        }

        
        protected void DoCleanOldSecurities()
        { 
            //TODO: DEV método de limpieza de securities que no se estuvieron usando
        
        }

        protected void DoSendMarketData(Wrapper wrapper)
        {
            OnMessageRcv(wrapper);
        
        }

        protected  void DoRequestMarketDataThread(object param)
        {
            Security sec = (Security)((object[])param)[0];
            SettlType settlType = (SettlType)((object[])param)[1];

            try
            {
                DoLog(string.Format("@{0}:Requesting market data por symbol {1}", IOLConfiguration.Name, sec.Symbol), Main.Common.Util.Constants.MessageType.Information);

                bool activo = true;
                while (activo)
                {
                    Thread.Sleep(IOLConfiguration.PublishUpdateInMilliseconds);

                    lock (tLock)
                    {
                        if (ActiveSecurities.Values.Where(x => x.Active).Any(x => x.Symbol == sec.Symbol))
                        {
                            //Probamos la versión derecha del mercado
                            try
                            {
                                string iolSymbol = SymbolConverter.GetCleanSymbolFromFullSymbol(sec.Symbol);
                                string iolExchange = ExchangeConverter.GetMarketFromFullSymbol(sec.Symbol);

                                if (IOLMarketDataManager != null)
                                {
                                    Common.DTO.MarketData marketData = (Common.DTO.MarketData) IOLMarketDataManager.GetMarketData(iolSymbol, iolExchange, settlType);
                                    if (marketData != null)
                                    {
                                        LoadMarketData(sec, settlType, marketData);
                                        InvertirOnlineMarketDataWrapper wrapper = new InvertirOnlineMarketDataWrapper(sec, IOLConfiguration);

                                        Task.Run(() => DoSendMarketData(wrapper));
                                    }
                                    else
                                        DoLog(string.Format("No response requesting MD for security {0}", sec.Symbol), Main.Common.Util.Constants.MessageType.Information);
                                }
                            }
                            catch (Exception ex)
                            {
                                DoLog(string.Format("@{0}:Error Requesting market data for symbol {1}:{2}",
                                    IOLConfiguration.Name, sec.Symbol, ex.Message), Main.Common.Util.Constants.MessageType.Information);
                            }
                        }
                        else
                        {
                            DoLog(string.Format("@{0}:Unsubscribing market data for symbol {1}", IOLConfiguration.Name, sec.Symbol), Main.Common.Util.Constants.MessageType.Information);

                            activo = false;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}: Error Requesting market data por symbol {1}:{2}", IOLConfiguration.Name, sec.Symbol, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }

        }
        protected Security BuildSecurityFromInstruction(string symbol)
        {

            Security sec = new Security()
            {
                Symbol = symbol,
                SecType = SecurityType.CS
            };

            return sec;
        }

        protected void ProcessPositionInstruction(Instruction instr)
        {
            try
            {

                if (instr != null)
                {
                    if (!ActiveSecurities.Keys.Contains(instr.Id) //ya no pedimos la instrx
                        && !ActiveSecurities.Values.Where(x => x.Active).Any(x => x.Symbol == instr.Symbol)//ya no estamos recuperando el market data del symbol
                        )
                    {
                        instr = InstructionManager.GetById(instr.Id);

                        if (instr.InstructionType.Type == InstructionType._NEW_POSITION || instr.InstructionType.Type == InstructionType._UNWIND_POSITION)
                        {
                            ActiveSecurities.Add(instr.Id, BuildSecurityFromInstruction(instr.Symbol));
                            RequestMarketDataThread = new Thread(DoRequestMarketDataThread);
                            RequestMarketDataThread.Start(new object[] { new Security() { Symbol = instr.Symbol, SecType = SecurityType.CS }, SettlType.Tplus2 });

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

        protected void DoFindInstructions()
        {
            while (true)
            {
                //We don't want to use Instructions as the input for requests
                if (InstructionManager == null)
                    return;

                Thread.Sleep(IOLConfiguration.PublishUpdateInMilliseconds);

                lock (tLock)
                {
                    List<Instruction> instructionsToProcess = InstructionManager.GetPendingInstructions(IOLConfiguration.AccountNumber);

                    try
                    {
                        foreach (Instruction instr in instructionsToProcess.Where(x => x.InstructionType.Type == InstructionType._NEW_POSITION 
                                || x.InstructionType.Type == InstructionType._UNWIND_POSITION))
                        {
                            //We process the account positions sync instructions
                            ProcessPositionInstruction(instr);
                        }
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("Critical error processing instructions: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : "")), Main.Common.Util.Constants.MessageType.Error);
                    }
                }

            }
        }

        protected void RequestMarketDataOnDemand(MarketDataRequest mdr, bool snapshot, string mode)
        {
            //zHFT.MarketClient.IB.Common.Configuration.Contract ctr = new MarketClient.IB.Common.Configuration.Contract();

            //ctr.Currency = sec.Currency;
            //ctr.Exchange = sec.Exchange;
            //ctr.SecType = zHFT.InstructionBasedMarketClient.IB.Common.Converters.SecurityConverter.GetSecurityType(sec.SecType);
            //ctr.Symbol = sec.Symbol;

            if (!ActiveSecurities.Values.Any(x => x.Symbol == mdr.Security.Symbol)
                && !ActiveSecuritiesOnDemand.Values.Any(x => x.Symbol == mdr.Security.Symbol))
            {
                DoLog(string.Format("@{0}:Requesting {2} Market Data On Demand for Symbol: {1}", IOLConfiguration.Name, mdr.Security.Symbol, mode), Main.Common.Util.Constants.MessageType.Information);
                ActiveSecurities.Add(ActiveSecurities.Keys.Count + 1, mdr.Security);
                RequestMarketDataThread = new Thread(DoRequestMarketDataThread);
                RequestMarketDataThread.Start(new object[] { mdr.Security, mdr.SettlType });

            }
            else
                DoLog(string.Format("@{0}:Market data already subscribed for symbol: {1}", IOLConfiguration.Name, mdr.Security.Symbol), Main.Common.Util.Constants.MessageType.Information);
        }

        protected void CancelMarketData(Security sec)
        {
            if (ActiveSecurities.Values.Any(x => x.Symbol == sec.Symbol))
            {
                foreach (Security s in ActiveSecurities.Values)
                {
                    if (sec.Symbol == s.Symbol)
                        s.Active = false;
                }
              
                DoLog(string.Format("@{0}:Requesting Unsubscribe Market Data On Demand for Symbol: {0}", IOLConfiguration.Name, sec.Symbol), Main.Common.Util.Constants.MessageType.Information);
                //TODO DEV: Cancel Market Data
            }
            else if (ActiveSecuritiesOnDemand.Values.Any(x => x.Symbol == sec.Symbol))
            {

                foreach (Security s in ActiveSecuritiesOnDemand.Values)
                {
                    if (sec.Symbol == s.Symbol)
                        s.Active = false;
                }

                DoLog(string.Format("@{0}:Requesting Unsubscribe Market Data On Demand for Symbol: {0}", IOLConfiguration.Name, sec.Symbol), Main.Common.Util.Constants.MessageType.Information);
                //TODO DEV: Cancel Market Data
            }
            else
                throw new Exception(string.Format("@{0}: Could not find active security to unsubscribe for symbol {1}", IOLConfiguration.Name, sec.Symbol));

        }

        protected CMState ProessMarketDataRequest(Wrapper wrapper)
        {
            try
            {
                MarketDataRequest mdr = MarketDataRequestConverter.GetMarketDataRequest(wrapper);

                if (mdr.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    throw new Exception(string.Format("@{0}: Market Data snaphsot not implemented for symbol {1}", IOLConfiguration.Name, mdr.Security.Symbol));
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.SnapshotAndUpdates)
                {

                    RequestMarketDataOnDemand(mdr, false, "Snapshot+Updates");
                    return CMState.BuildSuccess();

                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.Unsuscribe)
                {
                    CancelMarketData(mdr.Security);
                    return CMState.BuildSuccess();
                }
                else
                    throw new Exception(string.Format("@{0}: Value not recognized for subscription type {1} for symbol {2}", IOLConfiguration.Name, mdr.SubscriptionRequestType.ToString(), mdr.Security.Symbol));
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        protected void CleanPrevInstructions()
        {
            List<Instruction> prevInstrx = InstructionManager.GetPendingInstructions(IOLConfiguration.AccountNumber);

            foreach (Instruction prevInstr in prevInstrx)
            {
                prevInstr.Executed = true;
                prevInstr.AccountPosition = null;
                InstructionManager.Persist(prevInstr);
            }

        }

        #endregion

        #region Public Methods

        public  bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string moduleConfigFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(moduleConfigFile))
                {
                    

                    ActiveSecurities = new Dictionary<int, Security>();
                    ActiveSecuritiesOnDemand = new Dictionary<int, Security>();
                    ContractsTimeStamps = new Dictionary<int, DateTime>();

                    if (!string.IsNullOrEmpty(IOLConfiguration.InstructionsAccessLayerConnectionString))
                    {
                        InstructionManager = new InstructionManager(IOLConfiguration.InstructionsAccessLayerConnectionString);
                        CleanPrevInstructions();
                    
                    }
                    

                    if (IOLConfiguration.Mode == Configuration._AUTPORTFOLIO)
                    {

                        IOLMarketDataManager = new IOLMarketDataManager(this.OnLogMsg, IOLConfiguration.CredentialsAccountNumber,
                                                                        IOLConfiguration.ConfigConnectionString,
                                                                        IOLConfiguration.MainURL);
                    }
                    else if (IOLConfiguration.Mode == Configuration._SINGLE)
                    {
                        IOLMarketDataManager = new IOLStandaloneMarketDataManager(this.OnLogMsg, IOLConfiguration.MainURL, IOLConfiguration.User, IOLConfiguration.Password);
                    }
                    else
                        throw new Exception(string.Format("Unknown IOL client mode: {0}", IOLConfiguration.Mode));

                  
                    CleanOldSecuritiesThread = new Thread(DoCleanOldSecurities);
                    CleanOldSecuritiesThread.Start();

                    ProcessInstructionsThread = new Thread(DoFindInstructions);
                    ProcessInstructionsThread.Start();

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + moduleConfigFile, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + moduleConfigFile + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        public  CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    if (Actions.MARKET_DATA_REQUEST == action)
                    {
                        return ProessMarketDataRequest(wrapper);
                    }
                    else
                    {
                        DoLog(string.Format("@{0}:Sending message {1} not implemented", IOLConfiguration.Name, action.ToString()), Main.Common.Util.Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception(string.Format("@{0}:Sending message {1} not implemented", IOLConfiguration.Name, action.ToString())));
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
