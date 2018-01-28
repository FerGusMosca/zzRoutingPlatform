
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

namespace zHFT.OrderRouters.Binance
{
    public class OrderRouter : BaseOrderRouter
    {
        #region Protected Attributes

        protected Common.Configuration.Configuration BinanceConfiguration { get; set; }

        protected AccountBinanceDataManager AccountBinanceDataManager { get; set; }

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

        #endregion

        #region Protected OrderRouterBase Methods

        protected ExecutionReportDTO RunGetOrder(Order order)
        {
            var apiClient = new ApiClient(BinanceConfiguration.ApiKey, BinanceConfiguration.Secret);
            var binanceClient = new BinanceClient(apiClient);

            string fullSymbol = order.Symbol + BinanceConfiguration.QuoteCurrency;

            var execReportResp = binanceClient.GetOrder(fullSymbol, Convert.ToInt64(order.OrderId)); ;

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

        protected void DoEvalExecutionReport()
        {
            try
            {
                bool active = true;
                while (active)
                {
                    Thread.Sleep(BinanceConfiguration.RefreshExecutionReportsInMilisec);
                    List<ExecutionReportWrapper> wrappersToPublish = new List<ExecutionReportWrapper>();
                    lock (tLock)
                    {
                        List<string> orderIdToRemove = new List<string>();

                        foreach (string orderId in ActiveOrders.Keys)
                        {

                            Order order = ActiveOrders[orderId];
                            ExecutionReportDTO execReportDTO = RunGetOrder(order);

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
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error processing execution reports!:{1}", BinanceConfiguration.Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected void RunNewOrder(Order order)
        {
            DoLog(string.Format("@{0}:Routing new order for symbol {1}", BinanceConfiguration.Name, order.Symbol), Main.Common.Util.Constants.MessageType.Information);

            var apiClient = new ApiClient(BinanceConfiguration.ApiKey, BinanceConfiguration.Secret);
            var binanceClient = new BinanceClientProxy(apiClient);

            string fullSymbol = order.Symbol + BinanceConfiguration.QuoteCurrency;

            var resp = binanceClient.PostNewLimitOrder(fullSymbol,
                                                                 Convert.ToDecimal(order.OrderQty.Value),
                                                                 Convert.ToDecimal(order.Price.Value),
                                                                 order.Side == Side.Buy ? OrderSide.BUY : OrderSide.SELL,
                                                                 order.DecimalPrecission
                                                                 );

            var newOrderResp = resp.Result;

            if (newOrderResp != null)
            {
                order.OrderId = newOrderResp.OrderId.ToString();
                ActiveOrders.Add(newOrderResp.OrderId.ToString(), order);
                OrderIdMappers.Add(order.ClOrdId, newOrderResp.OrderId.ToString());
            }
            else
                throw new Exception(string.Format("Unknown error routing order for currency {0}", order.Symbol));
        
        }

        protected void EvalRouteError(Order order, Exception ex)
        {
            ExecutionReportDTO execReport = GetTheoreticalResponseOrderNotFound(order);
            order.OrdStatus = OrdStatus.Rejected;
            order.RejReason = ex.Message;

            ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, execReport);
            OnMessageRcv(wrapper);
        }

        protected void EvalNewOrderError(Order order, Exception ex)
        {
            order.OrdStatus = OrdStatus.Rejected;

            if (order.Side == Side.Buy)
            {
                order.RejReason = "Possible min ammount not enough @Binance. Error: " + ex.Message;
            }
            else
            {
                order.RejReason = "Possible rounding error for Qty @Binance. Error: " + ex.Message;
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

            var apiClient = new ApiClient(BinanceConfiguration.ApiKey, BinanceConfiguration.Secret);
            var binanceClient = new BinanceClient(apiClient);

            string fullSymbol = order.Symbol + BinanceConfiguration.QuoteCurrency;

            var resp = binanceClient.CancelOrder(fullSymbol, Convert.ToInt64(order.OrderId));

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
                    return false;//hubo un error, no hay nada que se pueda cancelar.
                                 //Lo que haya pasado será analizado por el thread de Execution Report
                }
            }
            else
            {
                try
                {
                    var updatedOrder = resp.Result;
                    ActiveOrders.Remove(order.OrderId);//si llegó hasta aca es porque actualizó bien
                    //Ya no actuallizamos mas datos de la vieja orden cancelada
                    return true;
                }
                catch (Exception)
                {
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
                                    EvalRouteError(order, ex);
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
                DoLog(string.Format("@{0}:Error updating order {1}!:{2}", BinanceConfiguration.Name, origClOrderId, ex.Message), Main.Common.Util.Constants.MessageType.Error);
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
                        throw new Exception(string.Format("Could not cancel order for id {0}", origClOrderId));
                        //TO DO: La orden fue modificada
                        //buscar con el nuevo clOrderId
                    }
                }
                return CMState.BuildSuccess();

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error cancelig order {1}!:{2}", BinanceConfiguration.Name, origClOrderId, ex.Message), Main.Common.Util.Constants.MessageType.Error);
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

                    AccountBinanceDataManager = new AccountBinanceDataManager(BinanceConfiguration.ConfigConnectionString);

                    OrderIdMappers = new Dictionary<string, string>();

                    ExecutionReportThread = new Thread(DoEvalExecutionReport);
                    ExecutionReportThread.Start();

                    //Todo inicializar mundo Bittrex
                    AccountBinanceData binanceData = AccountBinanceDataManager.GetByAccountNumber(BinanceConfiguration.AccountNumber);

                    if (binanceData == null)
                        throw new Exception(string.Format("No se encontró ninguna configuración de autenticación contra Binance de la cuenta {0}", BinanceConfiguration.AccountNumber));

                    BinanceConfiguration.ApiKey = binanceData.APIKey;
                    BinanceConfiguration.Secret = binanceData.Secret;

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
                DoLog(string.Format("@{0}:Critic error initializing " + configFile + ":" + ex.Message, BinanceConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        
        }

        #endregion
    }
}
