using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.ConsoleStrategy.Common.Configuration;
using tph.ConsoleStrategy.Common.Util;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.BusinessEntities;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.DTO;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.DataAccessLayer;
using zHFT.StrategyHandler.LogicLayer;
using zHFT.StrategyHandlers.Common.Converters;
using static zHFT.Main.Common.Util.Constants;

namespace tph.ConsoleStrategy.LogicLayer
{
    public class ConsoleStrategy : DayTradingStrategyBase
    {

        #region Protected Static Consts

        protected static string _DATE_FORMAT = "MM/dd/yyyy";

        #endregion

        #region Protected Attributes

        protected Dictionary<string, Wrapper> MarketDataRequests { get; set; }

        protected Dictionary<string, Position> RoutedPosDict { get; set; }

        protected Dictionary<string ,MarketData > MarketDataDict { get; set; }

        protected PositionIdTranslator PositionIdTranslator { get; set; }

        protected static int _POS_ID_LENGTH = 36;

        protected ICommunicationModule EconomicDataModule { get; set; }

        protected CandleManager CandleManager { get; set; }


        #endregion

        #region Command Methods


        protected void DoRequestMarketData(string[] param)
        {
            string cmd = param[0];

            CommandValidator.ValidateCommandParams(cmd, param, 4, 7);

            string symbol = CommandValidator.ExtractMandatoryParam(param, 1);

            string currency = CommandValidator.ExtractNonMandatoryParam(param, 4,def: Config.Currency);
            string exchange = CommandValidator.ExtractNonMandatoryParam(param, 5,def: Config.Exchange);
            string strSecType = CommandValidator.ExtractNonMandatoryParam(param, 6,def:Config.SecurityTypes);
            SecurityType? secType = SecurityTypeTranslator.TranslateNonMandatorySecurityType(strSecType);

            (new Thread(RequestMarketDataAsync)).Start(new object[] { symbol, currency, exchange, secType });

        }

       

        protected virtual Position LoadNewRegularPos(Security sec, Side side,QuantityType qtyType, double qty,string accountId)
        {

            Position pos = new Position()
            {

                Security = sec,
                Exchange=sec.Exchange,
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = qtyType == QuantityType.CURRENCY ? (double?)qty : null,
                Qty= qtyType == QuantityType.SHARES ? (double?)qty : null,
                QuantityType = qtyType,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = accountId,


            };

            pos.LoadPosGuid(PositionIdTranslator.GetNextGuidPosId());

            return pos;
        }

      

        public CMState ProcessIncoming(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {

                    ProcessMarketData(wrapper);
                    return CMState.BuildSuccess();
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
            string currency = CommandValidator.ExtractNonMandatoryParam(param, 4,def:Config.Currency);
            string exchange = CommandValidator.ExtractNonMandatoryParam(param, 5,def:Config.Exchange);
            string strSecType = CommandValidator.ExtractNonMandatoryParam(param, 6,def:Config.SecurityTypes);
            string accountId = CommandValidator.ExtractNonMandatoryParam(param, 7, def: "");
            SecurityType? secType = SecurityTypeTranslator.TranslateNonMandatorySecurityType(strSecType);


            Position pos = null;
            lock (RoutedPosDict)
            {
                Security sec = GetSecurity(symbol, currency, exchange, secType);
                pos = LoadNewRegularPos(sec, side, qtyType, qty, accountId);

                DoLog($"ROUTING {symbol} Pos for Symbol {0} Side={strSide} CashQty={qty.ToString("0.00")}", Constants.MessageType.PriorityInformation);
                RoutedPosDict.Add(pos.PosId, pos);

            }

            DoOpenTradingRegularPos(pos, null);

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

                        int friendlyPosId = PositionIdTranslator.GetFriendlyPosId(cxlPos.PosId);
                        DoLog($"Sending cancelation for PosId {friendlyPosId} ({cxlPos.PosId})", MessageType.PriorityInformation);
                        OrderRouter.ProcessMessage(cxlWrapper);


                    }
                    else
                    {
                        int friendlyPosId = PositionIdTranslator.GetFriendlyPosId(cxlPos.PosId);
                        DoLog($"Ignoring position {friendlyPosId} ({cxlPos.PosId}) because it is in status {cxlPos.PosStatus}", MessageType.PriorityInformation);
                    }
                    
                }


              
            }
        }

        protected void UnwindPos(string[] param)
        {
            string mainCmd = param[0];
            CommandValidator.ValidateCommandParams(mainCmd, param, 2, 2);

            int friendlyPosId = Convert.ToInt32(param[1]) ;


            lock (RoutedPosDict)
            {
                string guidPosId = PositionIdTranslator.GetRealPosId(friendlyPosId);
                if (RoutedPosDict.ContainsKey(guidPosId))
                {
                    Position unwindPos = RoutedPosDict[guidPosId];

                    if (unwindPos.PositionRouting())
                    {
                        DoLog($"Position is being routed to the exchange {friendlyPosId} ({guidPosId}) (symbol={unwindPos.Security.Symbol} status={unwindPos.PosStatus}) --> Cancel the positions first!", MessageType.PriorityInformation);
                    }
                    else if (unwindPos.FilledPos() || (unwindPos.CanceledPos() && unwindPos.CumQty>0))
                    {
                        Position newPos = unwindPos.Clone();

                        newPos.Qty = unwindPos.CumQty;
                        newPos.QuantityType = QuantityType.SHARES;
                        newPos.Side = unwindPos.FlipSide();
                        newPos.LoadPosGuid(PositionIdTranslator.GetNextGuidPosId());
                        //newPos.LoadPosId(NextPosId);
                        newPos.PosStatus = PositionStatus.PendingNew;

                        PositionWrapper posWrapper = new PositionWrapper(newPos, Config);

                        DoLog($"Unwinding position w/ PosId {newPos.PosId} Side={unwindPos.Side}->{newPos.Side} Qty={newPos.Qty}", MessageType.PriorityInformation);
                        OrderRouter.ProcessMessage(posWrapper);
                    }
                }
                else
                    DoLog($"Could not find a position for PosId {friendlyPosId} ({guidPosId})", MessageType.Error);
            }
        }

        protected void CancelPosition(string[] param)
        {
            string mainCmd = param[0];
            CommandValidator.ValidateCommandParams(mainCmd, param, 2, 2);

            int friendlyPosId = Convert.ToInt32(param[1]);
            string guidPosId = PositionIdTranslator.GetRealPosId(friendlyPosId);

            lock (RoutedPosDict)
            {
                if (RoutedPosDict.ContainsKey(guidPosId))
                {
                    Position cxlPos = RoutedPosDict[guidPosId];

                    CancelPositionWrapper cxlWrapper= new CancelPositionWrapper(cxlPos, Config);

                    DoLog($"Sending cancelation for PosId {friendlyPosId}({guidPosId})", MessageType.PriorityInformation);
                    OrderRouter.ProcessMessage(cxlWrapper);
                }
                else
                    DoLog($"Could not find a position for PosId {friendlyPosId}({guidPosId})", MessageType.Error);
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
        protected void RequestMarketdata(string[] param)
        {
            try
            {
                string cmd = param[0];

                CommandValidator.ValidateCommandVariableParams(cmd, param, 2, 5);

                string symbol = CommandValidator.ExtractMandatoryParam(param, 1);
                string strSecType = CommandValidator.ExtractNonMandatoryParam(param, 2,def: Config.SecurityTypes);
                SecurityType? secType = SecurityTypeTranslator.TranslateNonMandatorySecurityType(strSecType);
                string currency = CommandValidator.ExtractNonMandatoryParam(param, 3,def:Config.Currency);
                string exchange = CommandValidator.ExtractNonMandatoryParam(param, 4,def:Config.Exchange);

                DoLog($"Requesting Market Data for Symbol={symbol} SecType={secType} Currency={currency} exchange={exchange}", MessageType.PriorityInformation);
                (new Thread(RequestMarketDataAsync)).Start(new object[] { symbol, currency, exchange, secType });

            }
            catch (Exception ex)
            {

                DoLog($"{Config.Name}--> CRITICAL ERROR Requesting Market Data:{ex.Message}", MessageType.Error);
            }

        }

        protected void GetEconomicSeries(string[] param)
        {
            try
            {
                string cmd = param[0];

                CommandValidator.ValidateCommandVariableParams(cmd, param, 2, 4);

                string seriesID = CommandValidator.ExtractMandatoryParam(param, 1);
                string strFrom = CommandValidator.ExtractNonMandatoryParam(param, 2);
                string strTo = CommandValidator.ExtractNonMandatoryParam(param, 3);

                DateTime from = CommandValidator.GetNonMandatoryDate(strFrom, _DATE_FORMAT, DateTime.MinValue);
                DateTime to = CommandValidator.GetNonMandatoryDate(strTo, _DATE_FORMAT, DateTime.Now);


                DoLog($"Requesting Economic Series for SeriesID={seriesID} from={strFrom} to={strTo} ", MessageType.PriorityInformation);

                //All the economic indicators are supposed to be daily values
                EconomicSeriesRequestWrapper wrapper = new EconomicSeriesRequestWrapper(seriesID, from, to,CandleInterval.DAY);
                (new Thread(RequestSeriesAsync)).Start(new object[] { wrapper });
            }
            catch (Exception ex)
            {

                DoLog($"{Config.Name}--> CRITICAL ERROR at GetEconomicSeries:{ex.Message}", MessageType.Error);
            }

        }


        protected void ParseEconomicSeries(string[] param)
        {
            try
            {
                string cmd = param[0];

                CommandValidator.ValidateCommandVariableParams(cmd, param, 2, 4);

                string path = CommandValidator.ExtractNonMandatoryParam(param, 1);
                string seriesID = CommandValidator.ExtractMandatoryParam(param, 2);
                string dateFormat = CommandValidator.ExtractMandatoryParam(param, 3);

                EconomicSeriesValue[] econSeries =  CSVFileReader.ReadCSVDataSeries(path, dateFormat);

                DoLog($"Found {econSeries.Length} records in {path} file", MessageType.Information);
                List<MarketData> observations = new List<MarketData>();
                foreach (EconomicSeriesValue obs in econSeries)
                {
                    MarketData obsMD = new MarketData();
                    obsMD.MDEntryDate = obs.Date;
                    obsMD.OpeningPrice = obs.Value;
                    obsMD.TradingSessionHighPrice = obs.Value;
                    obsMD.TradingSessionLowPrice = obs.Value;
                    obsMD.ClosingPrice = obs.Value;
                    obsMD.Trade = obs.Value;
                    obsMD.Security = new Security() { Symbol = seriesID, SecType = SecurityType.IND };

                    observations.Add(obsMD);
                }


                DoLog($"Persisting {observations.Count} records in the DB",MessageType.Information);
                foreach (MarketData obs in observations)
                {
                    DoLog($"@{Config.Name} --> Persisting value of {obs.Trade} and date {obs.GetReferenceDateTime()} for seriesID {seriesID} (Interval={CandleInterval.DAY})", Constants.MessageType.PriorityInformation);

                    CandleManager.Persist(seriesID, CandleInterval.DAY, obs);
                }
                DoLog($"{observations.Count} records for series {seriesID} successfully persisted in DB", MessageType.Information);
            }
            catch (Exception ex)
            {

                DoLog($"{Config.Name}--> CRITICAL ERROR at ParseEconomicSeries:{ex.Message}", MessageType.Error);
            }

        }

        //

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

                    Console.WriteLine($" PosId={PositionIdTranslator.GetFriendlyPosId(pos.PosId)} Symbol={pos.Security.Symbol} Side={pos.Side} Qty={strQty} QtyType={pos.QuantityType}" +
                                     $"  Status={pos.PosStatus} CumQty={pos.CumQty} LvsQty={pos.LeavesQty} AvgPx={pos.AvgPx}");
                
                }
                Console.WriteLine();
            
            
            }
        
        
        }


        #endregion

        #region Private Methods

        protected void ShowCommands()
        {
            Console.WriteLine();
            Console.WriteLine($"==========Trading Commands ==========");
            Console.WriteLine($"#1-NewPosCash <symbol> <side> <CashQty> <Currency*> <Echange*> <SecType*>");
            Console.WriteLine($"#2-NewPosQty <symbol> <side> <Qty> <Currency*> <Echange*> <SecType*>");
            Console.WriteLine($"#3-ListRoutedPositions ");
            Console.WriteLine($"#4-CancelPosition <PosId>");
            Console.WriteLine($"#5-CancelAll");
            Console.WriteLine($"#6-UnwindPos <PosId>");
            Console.WriteLine($"#7-RequestMarketData <symbol> <SecType*> <Currency*> <Echange*>");
            Console.WriteLine($"#8-GetEconomicSeries <SeriresID> <From*> <To*>");
            Console.WriteLine($"#9-ParseEconomicSeries <Path> <SeriesID> <DateFormat>");
            Console.WriteLine();
        }

        protected void ProcessCommands(string cmd)
        {
            string[] cmdArr = cmd.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            string mainCmd = cmdArr[0];

            try
            {

                if (mainCmd == "NewPosCash")
                {
                    ProcessNewPos(cmdArr, QuantityType.CURRENCY);
                }
                else if (mainCmd == "NewPosQty")
                {
                    ProcessNewPos(cmdArr, QuantityType.SHARES);
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
                else if (mainCmd == "UnwindPos")
                {
                    UnwindPos(cmdArr);
                }
                else if (mainCmd == "RequestMarketData")
                {
                    RequestMarketdata(cmdArr);
                }
                else if (mainCmd == "GetEconomicSeries")
                {
                    GetEconomicSeries(cmdArr);
                }
                else if (mainCmd == "ParseEconomicSeries")
                {
                    ParseEconomicSeries(cmdArr);
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
            catch(Exception ex)
            {
                DoLog($"ERROR Processing Command:{ex.Message}", MessageType.Error);


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
            catch (Exception ex)
            {

                DoLog($"{Config.Name}-->CRITICAL ERROR Reading command: {ex.Message}", MessageType.Error);
                Console.ReadKey();
            }

        }

        private Security GetSecurity(string symbol, string currency, string exchange, SecurityType? secType)
        {
            return new Security()
            {
                Symbol = symbol,
                Currency = currency,
                Exchange = exchange,
                SecType = secType.HasValue ? secType.Value : SecurityType.CS,
                
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

        protected void RequestSeriesAsync(object param)
        {
            Wrapper seriesReqWrapper = (Wrapper)((object[])param)[0];
            

            try
            {

                lock (MarketDataRequests)
                {
                    EconomicDataModule.ProcessMessage(seriesReqWrapper);
                }
            }
            catch (Exception ex)
            {
                DoLog($"CRITICAL ERROR Running Sries Request:{ex.Message}", MessageType.Error);

            }
        }

        #endregion

        #region Protected Methods


        protected void AssignMainERParameters(Position pos, ExecutionReport report)
        {
            pos.CumQty = report.CumQty;
            pos.LeavesQty = report.LeavesQty;
            pos.AvgPx = report.AvgPx.HasValue ? (double?)report.AvgPx.Value : null;
            pos.SetPositionStatusFromExecutionStatus(report.OrdStatus);
            pos.ExecutionReports.Add(report);

        }

        protected int LoadFirstPostId()
        {
            TimeSpan elapsed = DateTimeManager.Now - new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

            int posIdInit = Convert.ToInt32(elapsed.TotalMinutes);

            if (posIdInit > 1000)
            {

                return posIdInit - 1000;
            }
            else
                return posIdInit;

        
        
        }

        protected override void ProcessExecutionReport(object param)
        {
            Wrapper wrapper = (Wrapper)param;

            try
            {

                ExecutionReport report = ExecutionReportConverter.GetExecutionReport(wrapper, Config);


                if (report.Order != null)
                {

                    string clOrdId = report.Order.ClOrdId;

                    string posId = Position.ExtractPosIDPrefix(clOrdId,_POS_ID_LENGTH);

                    if (posId != null)
                    {
                        lock (RoutedPosDict)
                        {
                            if (RoutedPosDict.ContainsKey(posId))
                            {

                                Position pos = RoutedPosDict[posId];
                                AssignMainERParameters(pos, report);

                                DoLog($"{Config.Name}--> Recv ER: PosId:{posId} Symbol:{pos.Symbol} Status={report.OrdStatus} CumQty:{report.CumQty} LvsQty:{report.LeavesQty} AvgPx:{report.AvgPx}   ", MessageType.PriorityInformation);
                            }
                            else
                            {
                                DoLog($"{Config.Name}--> Ignorning not processed PosId {posId}", MessageType.PriorityInformation);
                            
                            }
                        }


                    }
                    else
                        DoLog($"{Config.Name}--> Could not recognize ClOrdId {clOrdId}", MessageType.PriorityInformation);



                }
                else
                {
                    DoLog($"{Config.Name}-->Recv unkwnon exec report w/no order ", MessageType.PriorityInformation);
                }

                DoLog($"Recv ER for symbol {report.Order.Symbol} w/Status ={report.OrdStatus} CumQty={report.CumQty} LvsQqty={report.LeavesQty} AvgPx={report.AvgPx})", Constants.MessageType.PriorityInformation);
                //TODO see if we can identify the routed position based on the ER order ClOrId

            }
            catch (Exception e)
            {
                DoLog($"{Config.Name}--> CRITICAL ERROR processing execution report:{e.Message}", MessageType.Error);
            }
        }


        #endregion

        #region ICommunicationModule, ILogger

        public override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            Config = ConfigLoader.GetConfiguration<ConsoleConfiguration>(this, configFile, listaCamposSinValor);
        }


        public ConsoleConfiguration GetConfig()
        {

            return (ConsoleConfiguration)Config;
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
                
                NextPosId = LoadFirstPostId();
                MarketDataRequests = new Dictionary<string, Wrapper>();
                RoutedPosDict=new Dictionary<string, Position>();
                MarketDataDict = new Dictionary<string, MarketData>();
                PositionIdTranslator = new PositionIdTranslator(1);
                ExecutionReportConverter = new ExecutionReportConverter();

                EconomicDataModule = LoadModules(GetConfig().EconomicDataModule, GetConfig().EconomicDataModuleConfigFile, pOnLogMsg);

                OrderRouter =LoadModules(Config.OrderRouter, Config.OrderRouterConfigFile, pOnLogMsg);

                CandleManager = new CandleManager(GetConfig().CandlesConnectionString);

                if (OrderRouter == null)
                    throw new Exception($"Could not initialize order router!:{Config.OrderRouter}");

                (new Thread(ReadCommand)).Start(null);
                return true;

            }
            else
            {
                return false;
            }
        }
    
        #region Base DayTradingModule Methods

        public override void InitializeManagers(string connStr)
        {
            //No managers-No DB
        }

        protected override void ProcessHistoricalPrices(object pWrapper)
        {
            //Not implemented
        }

        protected override void ProcessMarketData(object pWrapper)
        {
            Wrapper wrapper = (Wrapper)pWrapper;
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
        }

        protected override void DoPersist(PortfolioPosition trdPos)
        {
            //No managers no DB
        }

        protected override void ResetEveryNMinutes(object param)
        {
            
        }

        protected override void LoadPreviousTradingPositions()
        {
            
        }

        protected void ProcessEconomicSeriesAsync(object param)
        {

            Wrapper wrapper = (Wrapper)param;
            EconomicSeriesDTO seriesDTO = EconomicSeriesConverter.ConvertEconomicSeries(wrapper);
            try
            {
                if (!seriesDTO.Success)
                    throw new Exception(seriesDTO.Error);


                DoLog($"@{Config.Name} Persisting {seriesDTO.Values.Count} values for seriesID {seriesDTO.SeriesID} (Interval={seriesDTO.Interval})", Constants.MessageType.PriorityInformation);

                List<MarketData> observations = new List<MarketData>();
                foreach (EconomicSeriesValue obs in seriesDTO.Values)
                {
                    MarketData obsMD = new MarketData();
                    obsMD.MDEntryDate = obs.Date;
                    obsMD.OpeningPrice = obs.Value;
                    obsMD.TradingSessionHighPrice = obs.Value;
                    obsMD.TradingSessionLowPrice = obs.Value;
                    obsMD.ClosingPrice = obs.Value;
                    obsMD.Trade = obs.Value;
                    obsMD.Security = new Security() { Symbol = seriesDTO.SeriesID, SecType = SecurityType.IND };

                    observations.Add(obsMD);
                }

                foreach (MarketData obs in observations)
                {
                    DoLog($"@{Config.Name} --> Persisting value of {obs.Trade} and date {obs.GetReferenceDateTime()} for seriesID {seriesDTO.SeriesID} (Interval={seriesDTO.Interval})", Constants.MessageType.PriorityInformation);

                    CandleManager.Persist(seriesDTO.SeriesID, seriesDTO.Interval, obs);
                }


                DoLog($"@{Config.Name} Succesfully persisted {seriesDTO.Values.Count} values for seriesID {seriesDTO.SeriesID} (Interval={seriesDTO.Interval})", Constants.MessageType.PriorityInformation);

            }
            catch (Exception ex)
            {
                DoLog($"@{Config.Name} Critical error persisting economic indicator for seriesID {seriesDTO.SeriesID}: {ex.Message}", Constants.MessageType.Error);
            }

        }


        protected override zHFT.StrategyHandler.BusinessEntities.PortfolioPosition DoOpenTradingRegularPos(Position pos, MonitoringPosition portfPos)
        {
            PositionWrapper posWrapper = new PositionWrapper(pos, Config);
            OrderRouter.ProcessMessage(posWrapper);

            return null;

        }

        protected override zHFT.StrategyHandler.BusinessEntities.PortfolioPosition DoOpenTradingFuturePos(Position pos, MonitoringPosition portfPos)
        {
            throw new NotImplementedException();
        }

        protected override void LoadMonitorsAndRequestMarketData()
        {
            throw new NotImplementedException();
        }

        protected override CMState ProcessOutgoing(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                    DoLog($"Incoming message from order routing w/ Action {wrapper.GetAction()}: " + wrapper.ToString(), Constants.MessageType.Information);

                if (wrapper.GetAction() == Actions.ECONOMIC_SERIES)
                {
                    (new Thread(ProcessEconomicSeriesAsync)).Start(wrapper);
                    return CMState.BuildSuccess();
                }
               else
                    return base.ProcessOutgoing(wrapper);
            }
            catch (Exception ex)
            {

                DoLog("Error processing message from order routing: " + (wrapper != null ? wrapper.ToString() : "") + " Error:" + ex.Message, Constants.MessageType.Error);

                return CMState.BuildFail(ex);
            }
        }

        #endregion



        #endregion
    }
}
