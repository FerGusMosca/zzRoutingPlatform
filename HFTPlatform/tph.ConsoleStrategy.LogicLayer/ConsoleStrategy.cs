using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.ConsoleStrategy.Common.Configuration;
using tph.ConsoleStrategy.Common.Util;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Wrappers;
using static zHFT.Main.Common.Util.Constants;

namespace tph.ConsoleStrategy.LogicLayer
{
    public class ConsoleStrategy : BaseCommunicationModule,ILogger
    {


        #region Protected Attributes

        public ConsoleConfiguration Config { get; set; }

        protected Dictionary<string, Wrapper> MarketDataRequests { get; set; }

        protected ICommunicationModule OrderRouter { get; set; }

        protected int NextPosId { get; set; }

        #endregion

        #region Command Methods


        protected void DoRequestMarketData(string[] param)
        {
            string cmd = param[0];

            CommandValidator.ValidateCommandParams(cmd, param, 4, 7);

            string symbol = CommandValidator.ExtractMandatoryParam(param, 1);

            string currency = CommandValidator.ExtractNonMandatoryParam(param, 4);
            string exchange = CommandValidator.ExtractNonMandatoryParam(param, 5);
            string strSecType = CommandValidator.ExtractNonMandatoryParam(param, 6);
            SecurityType? secType = SecurityTypeTranslator.TranslateNonMandatorySecurityType(strSecType);

            (new Thread(RequestMarketDataAsync)).Start(new object[] { symbol, currency, exchange, secType });

        }

        protected virtual Position LoadNewRegularPos(Security sec, Side side, double cashQty)
        {

            Position pos = new Position()
            {

                Security = sec,
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = cashQty,
                QuantityType = QuantityType.CURRENCY,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = "",
                

            };

            pos.LoadPosId(NextPosId);
            NextPosId++;

            return pos;
        }

        protected CMState ProcessMarketData(Wrapper wrapper)
        {
            OrderRouter.ProcessMessage(wrapper);

            return CMState.BuildSuccess();
        }

        public CMState ProcessIncoming(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {

                    return ProcessMarketData(wrapper);
                }
                else
                    return CMState.BuildFail(new Exception($"@{Config.Name}-->Could not process action {wrapper.GetAction().ToString()} "));
            }
            catch (Exception ex)
            {
                DoLog($"{Config.Name}-->Error processing market data: {ex.Message} ", Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        protected void DoLoadNewPos(string[] param)
        {
            string cmd = param[0];

            CommandValidator.ValidateCommandParams(cmd, param, 4, 7);

            string symbol = CommandValidator.ExtractMandatoryParam(param, 1);
            string strSide = CommandValidator.ExtractMandatoryParam(param, 2);
            Side side = SideTranslator.TranslateMandatorySide(strSide);
            double cashQty = CommandValidator.ExtractMandatoryDouble(param, 3);
            string currency = CommandValidator.ExtractNonMandatoryParam(param, 4);
            string exchange = CommandValidator.ExtractNonMandatoryParam(param, 5);
            string strSecType = CommandValidator.ExtractNonMandatoryParam(param, 6);
            SecurityType? secType = SecurityTypeTranslator.TranslateNonMandatorySecurityType(strSecType);


            Security sec = GetSecurity(symbol, currency, exchange, secType);
            Position pos = LoadNewRegularPos(sec, side, cashQty);

            DoLog($"ROUTING {symbol} Pos for Symbol {0} Side={strSide} CashQty={cashQty.ToString("0.00")}", Constants.MessageType.PriorityInformation);

            PositionWrapper posWrapper = new PositionWrapper(pos, Config);
        }

        protected void ProcessNewPos(string[] param)
        {
            try
            {
                DoRequestMarketData(param);
                DoLoadNewPos(param);
            }
            catch (Exception ex)
            {

                DoLog($"{Config.Name}--> CRITICAL ERROR Processing New Pos:{ex.Message}", MessageType.Error);
            }
        
        }

        #endregion

        #region Private Methods

        private Security GetSecurity(string symbol, string currency, string exchange, SecurityType? secType)
        {
            return new Security()
            {
                Symbol = symbol,
                Currency = currency,
                Exchange = exchange,
                SecType = secType.HasValue ? secType.Value : SecurityType.CS
            };
        }

        protected void RequestMarketDataAsync(object param)
        {
            object[] paramArr = (object[])param;
            string symbol = (string)paramArr[0];

            try
            {
                string currency = (string)paramArr[1];
                string exchange = (string)paramArr[2];
                SecurityType? secType = (SecurityType?)paramArr[3];

                lock (MarketDataRequests)
                {
                    Security sec = GetSecurity(symbol, currency, exchange, secType);
                    MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(sec, SubscriptionRequestType.SnapshotAndUpdates, sec.Currency);
                    OnMessageRcv(wrapper);
                }
            }
            catch (Exception ex)
            {
                DoLog($"CRITICAL ERROR Requesting market data for symbol {symbol}", MessageType.Error);
            
            }
        }

        #endregion

        #region Protected Methods

        protected CMState ProcessExecutionReport(Wrapper wrapper)
        {
            return CMState.BuildSuccess();
        }

        protected void ShowCommands()
        {
            Console.WriteLine($"==========Trading Commands ==========");
            Console.WriteLine($"NewPos <symbol> <side> <CashQty> <Currency*> <Echange*> <SecType*>");
        
        }

        protected void ProcessCommands(string cmd)
        {
            string[] cmdArr = cmd.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            string mainCmd = cmdArr[0];


            if (mainCmd == "NewPos")
            {
                ProcessNewPos(cmdArr);
            }
            else
            {
                DoLog($"Command not recognized: {mainCmd}", MessageType.Error);
            
            }
        }


        protected void ReadCommand(object param)
        {
            try
            {

                while (true)
                { 
                    ShowCommands();

                    string cmd = Console.ReadLine();

                    ProcessCommands(cmd);
                }
            }
            catch(Exception ex)
            {

                DoLog($"{Config.Name}-->CRITICAL ERROR Reading command: {ex.Message}", MessageType.Error);
                Console.ReadKey();
            }
        
        }

        #endregion

        #region ICommunicationModule, ILogger

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            Config = ConfigLoader.GetConfiguration<ConsoleConfiguration>(this, configFile, listaCamposSinValor);
        }

        void ILogger.DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            DoLoadConfig(configFile, listaCamposSinValor);
        }

        public void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            this.OnMessageRcv += pOnMessageRcv;
            this.OnLogMsg += pOnLogMsg;

            if (ConfigLoader.LoadConfig(this, configFile))
            {
                NextPosId = 1;
                MarketDataRequests = new Dictionary<string, Wrapper>();

                OrderRouter = LoadModule(Config.OrderRouter, "Order Router Module");
                OrderRouter.Initialize(ProcessOutgoing, pOnLogMsg, Config.OrderRouterConfigFile);


                (new Thread(ReadCommand)).Start(null);
                return true;

            }
            else
            {
                return false;
            }
        }


        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {

                    return ProcessMarketData(wrapper);
                }
                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), Config.Name)));
            }
            catch (Exception ex)
            {
                DoLog($"{Config.Name}-->Error processing market data: {ex.Message} ", Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        public  CMState ProcessOutgoing(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.EXECUTION_REPORT)
                {
                    //SDoLog("Processing Market Data:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);
                    ProcessExecutionReport(wrapper);
                    return CMState.BuildSuccess();
                }
                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), Config.Name)));
            }
            catch (Exception ex)
            {
                DoLog($"ERROR @ProcessMessage {Config.Name} : {ex.Message}", MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }



        #endregion
    }
}
