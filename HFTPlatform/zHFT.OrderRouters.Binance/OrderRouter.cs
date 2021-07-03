
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Objects.Spot.MarketData;
using CryptoExchange.Net.Objects;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Binance.BusinessEntities;
using zHFT.OrderRouters.Binance.Common.DTO;
using zHFT.OrderRouters.Binance.Common.Wrappers;
using zHFT.OrderRouters.Binance.DataAccessLayer.Managers;
using zHFT.OrderRouters.BINANCE.Common.Util;
using zHFT.OrderRouters.Cryptos;
using BinanceClient2 = Binance.Net.BinanceClient;
using Constants = zHFT.Main.Common.Util.Constants;
using Side = zHFT.Main.Common.Enums.Side;

namespace zHFT.OrderRouters.Binance
{
    public class OrderRouter : BaseOrderRouter
    {
        #region Protected Attributes

        protected Common.Configuration.Configuration BinanceConfiguration { get; set; }

        protected AccountBinanceDataManager AccountBinanceDataManager { get; set; }
        
        protected AccountBinanceData AccountBinanceData { get; set; }
        
        protected  BinanceClient BinanceClient { get; set; }
        
        protected Dictionary<string, Order> JustSentOrders { get; set; }
        //When we sent a new order to Binance, it might not be available for reading ERs
        // just after a few seconds. So this dictionary will be used to avoid sending unecessary errors
        
        #endregion

        #region Overriden Methods

        protected override BaseConfiguration GetConfig()
        {
            return BinanceConfiguration;
        }

        protected override string GetQuoteCurrency()
        {
            return BinanceConfiguration.QuoteCurrency;
        }

        protected override void DoLoadConfig(string configFile, List<string> noValueFields)
        {
            BinanceConfiguration = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        protected override CMState ProcessSecurityList(Wrapper wrapper)
        {
            return CMState.BuildSuccess();
        }

        #endregion

        #region Protected OrderRouterBase Methods
        
        protected void BuildBinanceData()
        {
            if (BinanceConfiguration.AccountNumber.HasValue)
            {
                AccountBinanceDataManager = new AccountBinanceDataManager(BinanceConfiguration.ConfigConnectionString);
                AccountBinanceData  = AccountBinanceDataManager.GetByAccountNumber(BinanceConfiguration.AccountNumber.Value);
            }
            else if(!string.IsNullOrEmpty(BinanceConfiguration.Secret) && !string.IsNullOrEmpty(BinanceConfiguration.Key))
            {
                AccountBinanceData  = new AccountBinanceData(){Secret = BinanceConfiguration.Secret,APIKey = BinanceConfiguration.Key};
            }
            else
                throw new Exception(String.Format("Could not find biannce keys. Not an account number or secret/key pair detected"));
            
        }

        private void LoadMemoryEntities()
        {
            var client = new BinanceClient2();
            
            
            WebCallResult<BinanceExchangeInfo> info = client.Spot.System.GetExchangeInfoAsync().Result;

            if (info.Success)
            {
                DecimalPrecissionConverter.ExchangeInfo = info.Data;
                DoLog(string.Format("Loading Binance Exchange info: {0} symbols found", 
                                        DecimalPrecissionConverter.ExchangeInfo.Symbols.Count()),Constants.MessageType.Information);
            }
            else
            {
                throw new Exception(info.Error.Message);
            }
        }

        protected ExecutionReportDTO RunGetExecutionReport(Order order)
        {
            string fullSymbol = order.Symbol + BinanceConfiguration.QuoteCurrency;

            var execReportResp = BinanceClient.GetOrder(fullSymbol, Convert.ToInt64(order.OrderId)); ;

            var execReport = execReportResp.Result;

            ExecutionReportDTO execReportDTO = new ExecutionReportDTO()
            {
                Order = order,
                ExecutedQty = execReport.ExecutedQty,
                LeavesQty = Convert.ToDecimal(order.OrderQty.Value) - execReport.ExecutedQty,
                OrigQty = execReport.OrigQty,
                Status = execReport.Status
            };

            return execReportDTO;
        }

        private ExecutionReportDTO GetTheoreticalResponseOrderNotFound(Order order)
        {
            decimal quantityRemaining = Convert.ToDecimal(order.OrderQty.Value);
            //evaluamos la orden como ejecutada o cancelada
            if (CanceledOrders.Contains(order.OrderId))
            {
                CanceledOrders.Remove(order.OrderId);
                quantityRemaining = 0;
            }

            return new ExecutionReportDTO()
            {
                Order = order,
                OrigQty = Convert.ToDecimal(order.OrderQty.Value),
                ExecutedQty = Convert.ToDecimal(order.OrderQty.Value) - quantityRemaining,
                Status = ExecutionReportDTO._CANCELED,
                Text = "Could not find order on exchange. Check on the exchange for possible execution or cancellation",
                LeavesQty = quantityRemaining,//0=cancelada, xx=ejecutada

            };
        }

        private ExecutionReportDTO GetTheoreticalResponseOrderUpdatingNotFound(Order order)
        {
            return new ExecutionReportDTO()
            {
                Order = order,
                OrigQty = Convert.ToDecimal(order.OrderQty.Value),
                ExecutedQty = 0,
                Status = ExecutionReportDTO._CANCELED,
                Text = "Could insert new order after cancellation for update. Check on the exchange for possible execution or cancellation",
                LeavesQty = Convert.ToDecimal(order.OrderQty.Value),//0=cancelada, xx=ejecutada

            };
        }

        private bool TryToGetExecutionReport(Order order,ref ExecutionReportDTO execReportDTO)
        {
            DoLog(string.Format("<Order Router> Fetching ER for OrderId {0}",order.OrderId),Constants.MessageType.Information);
            try
            {
                execReportDTO = RunGetExecutionReport(order);
                
                if (JustSentOrders.ContainsKey(order.OrderId))
                    JustSentOrders.Remove(order.OrderId);
            }
            catch (Exception e)
            {
                DoLog(string.Format("<Order Router> ERROR Fetching ER for OrderId {0}:{1}",order.OrderId,e.Message),Constants.MessageType.Error);

                if (JustSentOrders.ContainsKey(order.OrderId))
                {
                    DoLog(
                        string.Format("Waiting for order {0} to arrive to the exchange", order.OrderId),
                        Constants.MessageType.Information);
                    return false;
                }
                else
                    throw;
            }
                            
            DoLog(string.Format("DB-Found ER for OrderId {0}",order.OrderId),Constants.MessageType.Information);
            return true;
        }

        protected void DoEvalExecutionReport()
        {
           
            bool active = true;
            while (active)
            {
                Thread.Sleep(BinanceConfiguration.RefreshExecutionReportsInMilisec);
                List<ExecutionReportWrapper> wrappersToPublish = new List<ExecutionReportWrapper>();

                try
                {
                    lock (tLock)
                    {
                        List<string> orderIdToRemove = new List<string>();

                        foreach (string orderId in ActiveOrders.Keys)
                        {

                            Order order = ActiveOrders[orderId];
                            ExecutionReportDTO execReportDTO = null;
                            
                            if(!TryToGetExecutionReport(order,ref execReportDTO))
                                continue;
                            
                            if (execReportDTO != null)
                            {
                                ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, execReportDTO);
                                OrdStatus status = (OrdStatus)wrapper.GetField(ExecutionReportFields.OrdStatus);

                                if (Order.FinalStatus(status))
                                {
                                    orderIdToRemove.Add(orderId);
                                    DoLog(string.Format("@{0}:Removing Order For Status:{1}", BinanceConfiguration.Name, status.ToString()), Main.Common.Util.Constants.MessageType.Debug);
                                }

                                wrappersToPublish.Add(wrapper);
                            }
                            else
                            {
                                execReportDTO = GetTheoreticalResponseOrderNotFound(order);
                                ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, execReportDTO);
                                OrdStatus status = (OrdStatus)wrapper.GetField(ExecutionReportFields.OrdStatus);
                                wrappersToPublish.Add(wrapper);
                                orderIdToRemove.Add(orderId);
                                DoLog(string.Format("@{0}:Removing Order Because no order could be found on market for order id {1}", BinanceConfiguration.Name, orderId), Main.Common.Util.Constants.MessageType.Debug);
                            }
                        }
                        orderIdToRemove.ForEach(x => ActiveOrders.Remove(x));
                    }

                    wrappersToPublish.ForEach(x => OnMessageRcv(x));
                    wrappersToPublish.Clear();
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{0}:Error processing execution reports!:{1}", BinanceConfiguration.Name, BinanceErrorFormatter.ProcessErrorMessage(ex)), Main.Common.Util.Constants.MessageType.Error);
                }
            }
        }

        protected void RunNewOrder(Order order)
        {
            DoLog(string.Format("@{0}:Routing new order for symbol {1}", BinanceConfiguration.Name, order.Symbol), Main.Common.Util.Constants.MessageType.Information);

            string fullSymbol = order.Symbol + BinanceConfiguration.QuoteCurrency;

            decimal qty = 0;

            qty = DecimalPrecissionConverter.GetQuantity(order.Symbol,
                                                        BinanceConfiguration.QuoteCurrency,
                                                        Convert.ToDecimal(order.OrderQty.Value));
            
            
            DecimalPrecissionConverter.ValidateNewOrder(order.Symbol, BinanceConfiguration.QuoteCurrency,
                                                        qty, order.Price.Value);
            
            var resp = BinanceClient.PostNewOrder(fullSymbol,qty,
                                                        Convert.ToDecimal(order.Price.Value), 
                                                        order.Side == Side.Buy ? OrderSide.BUY : OrderSide.SELL);

            var newOrderResp = resp.Result;
            DoLog(string.Format("New order with OrderId {0} inserted for symbol {1}", newOrderResp.OrderId, fullSymbol),
                Constants.MessageType.Information);

            if (newOrderResp != null)
            {
                order.OrderId = newOrderResp.OrderId.ToString();
                JustSentOrders.Add(newOrderResp.OrderId.ToString(),order);
                ActiveOrders.Add(newOrderResp.OrderId.ToString(), order);
                OrderIdMappers.Add(order.ClOrdId, newOrderResp.OrderId.ToString());
            }
            else
                throw new Exception(string.Format("Unknown error routing order for currency {0}", order.Symbol));
        
        }

        // protected void EvalRouteError(Order order, Exception ex)
        // {
        //     ExecutionReportDTO execReport = GetTheoreticalResponseOrderNotFound(order);
        //     order.OrdStatus = OrdStatus.Rejected;
        //     order.RejReason = BinanceErrorFormatter.ProcessErrorMessage(ex);
        //
        //     ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, execReport);
        //     OnMessageRcv(wrapper);
        // }

        protected void EvalErrorOnInsertingUpdate(Order order, Exception ex)
        {
            ExecutionReportDTO execReport = GetTheoreticalResponseOrderUpdatingNotFound(order);
            order.OrdStatus = OrdStatus.Rejected;
            order.RejReason = BinanceErrorFormatter.ProcessErrorMessage(ex);

            ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, execReport);
            OnMessageRcv(wrapper);
        }

        protected void EvalNewOrderError(Order order, Exception ex)
        {
            order.OrdStatus = OrdStatus.Rejected;

            string message = "";
            if (ex.InnerException != null)
                order.RejReason = string.Format("error:{0}", ex.InnerException.Message);
            else
            {
                if (order.Side == Side.Buy)
                    order.RejReason = "Possible min ammount not enough @Binance. Error: " + BinanceErrorFormatter.ProcessErrorMessage(ex);
                else
                    order.RejReason = "Possible rounding error for Qty @Binance. Error: " + BinanceErrorFormatter.ProcessErrorMessage(ex);
            }

            order.OrderId = "not created";

            ExecutionReportDTO execReport = new ExecutionReportDTO
                                            {
                                                Order = order,
                                                OrigQty = Convert.ToDecimal(order.OrderQty.Value),
                                                ExecutedQty = 0,
                                                Status = ExecutionReportDTO._REJECTED,
                                                Text = order.RejReason,
                                                LeavesQty = 0,
                                            };

            ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, execReport);
            OnMessageRcv(wrapper);
        }

        protected override CMState RouteNewOrder(Wrapper wrapper)
        {
            try
            {
                Order order = GetOrder(wrapper);
                
                try
                {
                    lock (tLock)
                    {
                        RunNewOrder(order);
                    }
                }
                catch (Exception ex)
                {
                    EvalNewOrderError(order, ex);
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        protected override bool RunCancelOrder(Order order, bool update)
        {
           
            DoLog(string.Format("@{0}:Cancelling Order Id {1} for symbol {2}", BinanceConfiguration.Name, order.OrderId, order.Symbol), Main.Common.Util.Constants.MessageType.Information);

            string fullSymbol = order.Symbol + BinanceConfiguration.QuoteCurrency;
            
            var resp = BinanceClient.CancelOrder(fullSymbol, Convert.ToInt64(order.OrderId));

            if (!update)//es cancelación pura
            {
                try
                {
                    var canceledOrder = resp.Result;
                    CanceledOrders.Add(order.OrderId);//si llegó hasta aca es porque canceló bien
                    return true;
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("Error Cancelling<for cancel> OrderId {0}: {1}",order.OrderId,BinanceErrorFormatter.ProcessErrorMessage(ex)),Constants.MessageType.Information);

                    return false;//hubo un error, no hay nada que se pueda cancelar.
                                 //Lo que haya pasado será analizado por el thread de Execution Report
                }
            }
            else //es update
            {
                try
                {
                    var updatedOrder = resp.Result;
                    ActiveOrders.Remove(order.OrderId);//si llegó hasta aca es porque canceló bien
                    //Ya no actuallizamos mas datos de la vieja orden cancelada
                    return true;
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("Error Cancelling<for update> OrderId {0}: {1}",order.OrderId,BinanceErrorFormatter.ProcessErrorMessage(ex)),Constants.MessageType.Error);
                    return false;
                }
            }

        }

        protected override CMState UpdateOrder(Wrapper wrapper)
        {
            string origClOrderId = wrapper.GetField(OrderFields.OrigClOrdID).ToString();

            string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();
            try
            {
                if (wrapper.GetField(OrderFields.OrigClOrdID) == null)
                    throw new Exception("Could not find OrigClOrdID for order updated");

                if (wrapper.GetField(OrderFields.ClOrdID) == null)
                    throw new Exception("Could not find ClOrdId for new order");

                lock (tLock)
                {

                    if (OrderIdMappers.ContainsKey(origClOrderId))
                    {
                        string origUuid = OrderIdMappers[origClOrderId];
                        Order order = ActiveOrders[origUuid];

                        if (order != null)
                        {
                            //Cancelamos
                            DoLog(string.Format("DB-Cancelling OrderId {0} @Update",order.OrderId),Constants.MessageType.Information);

                            if (RunCancelOrder(order, true))
                            {
                                Thread.Sleep(100);

                                //Damos el alta
                                double? newPrice = (double?)wrapper.GetField(OrderFields.Price);
                                order.Price = newPrice;
                                order.ClOrdId = clOrderId;
                                try
                                {
                                    RunNewOrder(order);
                                }
                                catch (Exception ex)
                                {
                                    EvalErrorOnInsertingUpdate(order, ex);
                                }
                            }
                            else
                                DoLog(string.Format("@{0}:Discarding cancelation of order because order could not be found!", BinanceConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);

                        }

                    }
                    else
                        DoLog(string.Format("@{0}:Could not find order for origClOrderId  {1}!", BinanceConfiguration.Name, origClOrderId), Main.Common.Util.Constants.MessageType.Error);

                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error updating order {1}!:{2}", BinanceConfiguration.Name, origClOrderId, BinanceErrorFormatter.ProcessErrorMessage(ex)), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }

        }

        protected override CMState CancelOrder(Wrapper wrapper)
        {
            string origClOrderId = wrapper.GetField(OrderFields.OrigClOrdID).ToString();
            try
            {
                //New order id
                string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();

                lock (tLock)
                {

                    if (OrderIdMappers.ContainsKey(origClOrderId))
                    {
                        string orderId = OrderIdMappers[origClOrderId];
                        Order order = ActiveOrders[orderId];

                        if (order != null)
                        {
                            RunCancelOrder(order, false);
                        }
                        OrderIdMappers.Remove(origClOrderId);
                    }
                    else
                    {
                        string msg = string.Format("Order {0} not found!", origClOrderId);
                        OrderCancelRejectWrapper cxlRejWrapper=new OrderCancelRejectWrapper(origClOrderId,clOrderId,msg);
                        OnMessageRcv(cxlRejWrapper);
                        DoLog(string.Format("Error trying to cancel ClOrdId {0}:{1}", origClOrderId, msg),Constants.MessageType.Information);
                    }
                }
                return CMState.BuildSuccess();

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error cancelling order {1}!:{2}", BinanceConfiguration.Name, origClOrderId, BinanceErrorFormatter.ProcessErrorMessage(ex)), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        #endregion

        #region Public Methods

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.ModuleConfigFile = configFile;
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    tLock = new object();

                    ActiveOrders = new Dictionary<string, Order>();
                    CanceledOrders = new List<string>();

                    BuildBinanceData();
                    
                    LoadMemoryEntities();

                    OrderIdMappers = new Dictionary<string, string>();
                    JustSentOrders = new Dictionary<string, Order>();

                    ExecutionReportThread = new Thread(DoEvalExecutionReport);
                    ExecutionReportThread.Start();
                    
                    var apiClient = new ApiClient(AccountBinanceData.APIKey, AccountBinanceData.Secret);
                    BinanceClient = new BinanceClient(apiClient);

                    return true;
                }
                else
                {
                    DoLog(string.Format("@{0}:Error initializing config file " + configFile, BinanceConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Critic error initializing " + configFile + ":" + BinanceErrorFormatter.ProcessErrorMessage(ex), BinanceConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        
        }

        #endregion
    }
}
