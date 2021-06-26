using System;
using System.Collections.Generic;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting;
using tph.StrategyHandler.SimpleCommandReceiver.DataAccessLayer;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Configuration;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandlers.Common.Converters;

namespace tph.StrategyHandler.SimpleCommandReceiver
{
    public class SimpleCommandReceiver : BaseCommunicationModule, ILogger
    {
        #region Protected Attributes

        protected ICommunicationModule MarketDataModule { get; set; }

        protected ICommunicationModule OrderRouterModule { get; set; }

        protected WebSocketServer Server { get; set; }

        public tph.StrategyHandler.SimpleCommandReceiver.Common.Configuration.Configuration Config { get; set; }

        #endregion

        #region ICommunicationModule

        public void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        void ILogger.DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            DoLoadConfig(configFile, listaCamposSinValor);
        }


        protected ICommunicationModule DoLoadModule(string assembly, string configFile,string desc)
        {
            ICommunicationModule dest = null;
            DoLog(string.Format("Initializing {0} : {1} ",desc,assembly)  , Constants.MessageType.Information);
            if (!string.IsNullOrEmpty(assembly))
            {
                var typeOrderRouter = Type.GetType(assembly);
                if (typeOrderRouter != null)
                {
                    dest = (ICommunicationModule)Activator.CreateInstance(typeOrderRouter);
                    dest.Initialize(ProcessMessage, DoLog, configFile);
                }
                else
                    throw new Exception("assembly not found: " +assembly);
            }
            else
                DoLog(desc+" not found. It will not be initialized", Constants.MessageType.Error);

            return dest;
        }

        protected void LoadModules( )
        {
            MarketDataModule= DoLoadModule(Config.IncomingModule,Config.IncomingConfigPath,"Market Data Client");
            OrderRouterModule = DoLoadModule(Config.OutgoingModule,Config.OutgoingConfigPath,"Order Router");
        }

        protected CMState ProcessOrderBook(Wrapper wrapper)
        {
            try
            {
                
                MarketDataConverter conv= new MarketDataConverter();

                OrderBook orderBook = conv.GetOrderBook(wrapper, Config);
                
                OrderBookDTO dto = new OrderBookDTO(orderBook);
                
                DoLog(string.Format("Sending Order book for security {0} at {1}:{2}",orderBook.Security.Symbol,DateTime.Now,orderBook.GetTopOfBook()),Constants.MessageType.Information);
                
                Server.PublishEntity<OrderBookDTO>(dto);
                
                return CMState.BuildSuccess();
            
            }
            catch (Exception e)
            {
                DoLog(string.Format("Error processing order book:{0}}",e.Message),Constants.MessageType.Error);
                return CMState.BuildFail(e);
            }
        }

        protected CMState ProcessExecutionReport(Wrapper wrapper)
        {
            try
            {
                
                ExecutionReportConverter conv = new ExecutionReportConverter();

                ExecutionReport execReport =conv.GetExecutionReport(wrapper, Config);
                
                DoLog(string.Format("Sending Execution Report for security {0} (Status={1}) at {2}:{3}",execReport.Order.Symbol,execReport.OrdStatus,DateTime.Now,execReport.ToString()),Constants.MessageType.Information);
                
                ExecutionReportDTO dto = new ExecutionReportDTO(execReport);
                
                Server.PublishEntity<ExecutionReportDTO>(dto);
                
                return CMState.BuildSuccess();
            
            }
            catch (Exception e)
            {
                DoLog(string.Format("Error processing execution report:{0}",e.Message),Constants.MessageType.Error);
                return CMState.BuildFail(e);
            }
        }

        protected CMState ProcessMarketData(Wrapper wrapper)
        {
            try
            {
                
                MarketDataConverter conv= new MarketDataConverter();

                MarketData md = conv.GetMarketData(wrapper, Config);
                
                MarketDataDTO dto = new MarketDataDTO(md);
                
                DoLog(string.Format("Sending MarketData for security {0} at {1}:{2}",md.Security.Symbol,DateTime.Now,md.ToString()),Constants.MessageType.Information);
                
                Server.PublishEntity<MarketDataDTO>(dto);
                
                return CMState.BuildSuccess();
            
            }
            catch (Exception e)
            {
                DoLog(string.Format("Error processing order book:{0}",e.Message),Constants.MessageType.Error);
                return CMState.BuildFail(e);
            }
        }


        protected override void DoLoadConfig(string configFile, List<string> noValFields)
        {
            List<string> noValueFields = new List<string>();
            Config = new Configuration().GetConfiguration<tph.StrategyHandler.SimpleCommandReceiver.Common.Configuration.Configuration>(configFile, noValueFields);
        }

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            
            if (wrapper.GetAction() == Actions.ORDER_BOOK)
            {
                DoLog("Processing Order Book:" + wrapper.ToString(), Constants.MessageType.Information);
                return ProcessOrderBook(wrapper);
            }
            else if (wrapper.GetAction() == Actions.MARKET_DATA)
            {
                DoLog("Processing Market Data:" + wrapper.ToString(), Constants.MessageType.Information);
                return ProcessMarketData(wrapper);
            }
            else if (wrapper.GetAction() == Actions.EXECUTION_REPORT)
            {
                DoLog("Processing Execution Report:" + wrapper.ToString(), Constants.MessageType.Information);
                return ProcessExecutionReport(wrapper);
            }
            else if (wrapper.GetAction() == Actions.MARKET_DATA_REQUEST)
            {
                return MarketDataModule.ProcessMessage(wrapper);
            }
            else if (wrapper.GetAction() == Actions.NEW_ORDER)
            {
                return OrderRouterModule.ProcessMessage(wrapper);
            }
            else
            {
            
                OnMessageRcv(wrapper);
                DoLog(string.Format("Invoking OnMessageRcv @SimpleCommandReceiver for action {0}",wrapper.GetAction()),Constants.MessageType.Error);
                return CMState.BuildSuccess();    
            }
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    //Build the  trading modules
                    LoadModules();
                    
                    //Finish starting up the server
                    Server = new WebSocketServer(Config.WebSocketURL, this, ProcessMessage);
                    Server.Start();

                    DoLog("Websocket successfully initialized on URL:  " + Config.WebSocketURL, Constants.MessageType.Information);
                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Constants.MessageType.Error);
                return false;
            }
        }
        
        #endregion

        
    }
}