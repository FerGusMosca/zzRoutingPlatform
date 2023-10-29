using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.ConsoleStrategy.Common.Configuration;
using tph.ConsoleStrategy.Common.Util;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Converters;
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

        protected Dictionary<string, Position> RoutedPosDict { get; set; }

        protected Dictionary<string ,MarketData > MarketDataDict { get; set; }

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

        protected virtual Position LoadNewRegularPos(Security sec, Side side,QuantityType qtyType, double qty)
        {

            Position pos = new Position()
            {

                Security = sec,
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = qtyType == QuantityType.CURRENCY ? (double?)qty : null,
                Qty= qtyType == QuantityType.SHARES ? (double?)qty : null,
                QuantityType = qtyType,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = "",


            };

            pos.LoadPosId(NextPosId);
            NextPosId++;

            return pos;
        }

        protected CMState ProcessMarketData(Wrapper wrapper)
        {
            MarketData md = MarketDataConverter.ConvertMarketData(wrapper);
            lock (MarketDataDict)
            {

                if (!MarketDataDict.ContainsKey(md.Security.Symbol))
                {
                    DoLog($"Recv first MD for symbol {md.Security.Symbol}:{md.ToString()}", MessageType.PriorityInformation);
                    MarketDataDict.Add(md.Security.Symbol, md);
                }
                else
                {
                    MarketDataDict[md.Security.Symbol] = md;
                }

            }

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

        protected void DoLoadNewPos(string[] param,QuantityType qtyType)
        {
            string cmd = param[0];

            CommandValidator.ValidateCommandParams(cmd, param, 4, 7);

            string symbol = CommandValidator.ExtractMandatoryParam(param, 1);
            string strSide = CommandValidator.ExtractMandatoryParam(param, 2);
            Side side = SideTranslator.TranslateMandatorySide(strSide);
            double qty = CommandValidator.ExtractMandatoryDouble(param, 3);
            string currency = CommandValidator.ExtractNonMandatoryParam(param, 4);
            string exchange = CommandValidator.ExtractNonMandatoryParam(param, 5);
            string strSecType = CommandValidator.ExtractNonMandatoryParam(param, 6);
            SecurityType? secType = SecurityTypeTranslator.TranslateNonMandatorySecurityType(strSecType);


            Position pos = null;
            lock (RoutedPosDict)
            {
                Security sec = GetSecurity(symbol, currency, exchange, secType);
                pos = LoadNewRegularPos(sec, side, qtyType, qty);

                DoLog($"ROUTING {symbol} Pos for Symbol {0} Side={strSide} CashQty={qty.ToString("0.00")}", Constants.MessageType.PriorityInformation);
                RoutedPosDict.Add(pos.PosId, pos);

            }

            PositionWrapper posWrapper = new PositionWrapper(pos, Config);
            OrderRouter.ProcessMessage(posWrapper);
        }

        protected void CancelAll(string[] param)
        {

            lock (RoutedPosDict)
            {
                foreach (Position cxlPos in RoutedPosDict.Values)
                {

                    if (!cxlPos.PositionNoLongerActive())
                    {
                        CancelPositionWrapper cxlWrapper = new CancelPositionWrapper(cxlPos, Config);

                        DoLog($"Sending cancelation for PosId {cxlPos.PosId}", MessageType.PriorityInformation);
                        OrderRouter.ProcessMessage(cxlWrapper);

                    }
                    else
                    {
                        DoLog($"Ignoring position {cxlPos.PosId} because it is in status {cxlPos.PosStatus}", MessageType.PriorityInformation);
                    }
                    
                }


              
            }
        }

        protected void CancelPosition(string[] param)
        {
            string mainCmd = param[0];
            CommandValidator.ValidateCommandParams(mainCmd, param, 2, 2);

            string posId = param[1];

            lock (RoutedPosDict)
            {
                if (RoutedPosDict.ContainsKey(posId))
                {
                    Position cxlPos = RoutedPosDict[posId];

                    CancelPositionWrapper cxlWrapper= new CancelPositionWrapper(cxlPos, Config);

                    DoLog($"Sending cancelation for PosId {posId}", MessageType.PriorityInformation);
                    OrderRouter.ProcessMessage(cxlWrapper);
                }
                else
                    DoLog($"Could not find a position for PosId {posId}", MessageType.Error);
            }

        }

        protected void ProcessNewPos(string[] param,QuantityType qtyType)
        {
            try
            {
                DoRequestMarketData(param);
                DoLoadNewPos(param,qtyType);
            }
            catch (Exception ex)
            {

                DoLog($"{Config.Name}--> CRITICAL ERROR Processing New Pos:{ex.Message}", MessageType.Error);
            }
        
        }

        protected void ListRoutedPositions(string[] param)
        {
            lock (RoutedPosDict)
            {
                Console.WriteLine();
                Console.WriteLine("==============Listing Routed Positions==============");
                foreach (Position pos in RoutedPosDict.Values)
                {


                    string strQty = "?";
                    if (pos.IsMonetaryQuantity())
                    {
                        strQty = pos.CashQty.HasValue ? pos.CashQty.Value.ToString("0.##") : "?";
                    }
                    else if(pos.IsNonMonetaryQuantity())
                    {

                        strQty = pos.Qty.HasValue ? pos.Qty.Value.ToString("0.00") : "?";
                    }

                    Console.WriteLine($" PosId={pos.PosId} Symbol={pos.Security.Symbol} Side={pos.Side} Qty={strQty} QtyType={pos.QuantityType}" +
                                     $"  Status={pos.PosStatus} CumQty={pos.CumQty} LvsQty={pos.LeavesQty} AvgPx={pos.AvgPx}");
                
                }
                Console.WriteLine();
            
            
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
            Console.WriteLine();
            Console.WriteLine($"==========Trading Commands ==========");
            Console.WriteLine($"#1-NewPosCash <symbol> <side> <CashQty> <Currency*> <Echange*> <SecType*>");
            Console.WriteLine($"#2-NewPosQty <symbol> <side> <CashQty> <Currency*> <Echange*> <SecType*>");
            Console.WriteLine($"#3-ListRoutedPositions ");
            Console.WriteLine($"#4-CancelPosition <PosId>");
            Console.WriteLine($"#5-CancelAll");
            Console.WriteLine();
        }

        protected void ProcessCommands(string cmd)
        {
            string[] cmdArr = cmd.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            string mainCmd = cmdArr[0];


            if (mainCmd == "NewPosCash")
            {
                ProcessNewPos(cmdArr,QuantityType.CURRENCY);
            }
            else if (mainCmd == "NewPosQty")
            {
                ProcessNewPos(cmdArr,QuantityType.SHARES);
            }
            else if (mainCmd == "ListRoutedPositions")
            {
                ListRoutedPositions(cmdArr);
            }
            else if (mainCmd == "CancelPosition")
            {
                CancelPosition(cmdArr);
            }
            else if (mainCmd == "CancelAll")
            {
                CancelAll(cmdArr);
            }

            else if (mainCmd == "cls")
            {
                Console.Clear();
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
                RoutedPosDict=new Dictionary<string, Position>();
                MarketDataDict = new Dictionary<string, MarketData>();

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
