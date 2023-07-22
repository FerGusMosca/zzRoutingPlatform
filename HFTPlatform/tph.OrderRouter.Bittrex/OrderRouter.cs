using Bittrex.Data;
using Bittrex.Net.Clients;
using Bittrex.Net.Enums;
using Bittrex.Net.Objects.Models;
using Bittrex.Net.Objects.Models.Socket;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.OrderRouter.Bittrex.Common;
using tph.OrderRouter.Bittrex.Common.Wrappers;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.OrderRouters.Bittrex.Common.Configuration;
using zHFT.OrderRouters.Bittrex.DataAccessLayer.Managers;
using zHFT.OrderRouters.Cryptos;
using zHFT.StrategyHandler.IBR.Bittrex.BusinessEntities;
using static zHFT.Main.Common.Util.Constants;
using TimeInForce = Bittrex.Net.Enums.TimeInForce;

namespace tph.OrderRouter.Bittrex
{
    public class OrderRouter : BaseOrderRouter
    {

        #region Protected Attrbuts

        protected Configuration BittrexConfiguration { get; set; }

        protected AccountBittrexDataManager AccountBittrexDataManager { get; set; }

        protected BittrexRestClient BittrexRestClient { get; set; }

        protected BittrexSocketClient BittrexSocketClient { get; set; }

        #endregion

        #region Private Methods

        private void UpdateExecutionReport(DataEvent<BittrexOrderUpdate> data)
        {
            try 
            {
                ExecutionReportWrapper wrapper = null;
                lock (ActiveOrders)
                {
                    if (OrderIdMappers.ContainsKey(data.Data.Delta.ClientOrderId))
                    {
                        string clOrdId = OrderIdMappers[data.Data.Delta.ClientOrderId];

                        if (ActiveOrders.ContainsKey(clOrdId))
                        {
                            Order order = ActiveOrders[clOrdId];
                            DoLog($"Received exec report for order {data.Data.Delta.ClientOrderId} for symbol {order.Security.Symbol}--> Status={data.Data.Delta.Status} and Cum.Qty={data.Data.Delta.QuantityFilled}", MessageType.Information);
                            wrapper = new ExecutionReportWrapper(order, data.Data);
                            order.CumQty = data.Data.Delta.QuantityFilled;
                            order.OrderId = data.Data.Delta.Id;
                        }
                        else
                            DoLog($"WARNING - Could not find an order in memory for  ClOrdId {clOrdId}", MessageType.Information);
                    }
                    else
                        DoLog($"WARNING - Could not find an order in memory for internal ClOrdId {data.Data.Delta.ClientOrderId}", MessageType.Information);
                }

                if(wrapper!=null)
                    OnMessageRcv(wrapper);

            }
            catch(Exception ex)
            {
                DoLog($"@{BittrexConfiguration.Name}- ERROR Processing execution report: {ex.Message}",MessageType.Error);
            }
        }

        private void SubscribeExecutionReports()
        {
            BittrexSocketClient.SpotApi.SubscribeToOrderUpdatesAsync(data =>
            {
                UpdateExecutionReport(data);
            });
        }

        private void RunNewOrder(Order order, bool update)
        {
            string internalClOdId = Guid.NewGuid().ToString();
            string symbol = $"{order.Symbol}-{order.Currency}";
            DoLog($"@{BittrexConfiguration.Name}:Creating order Order for symbol {symbol}", MessageType.Information);
            zHFT.Main.Common.Enums.Side side = order.Side;
            decimal ordQty = 0;

            if (order.OrderQty.HasValue)
                ordQty = Convert.ToDecimal(order.OrderQty.Value);
            else
                throw new Exception($"Could not create an order without a qty for Symbol {symbol} at Bittrex Order Router");

            decimal price = 0;
            if (order.Price.HasValue)
                price = Convert.ToDecimal(order.Price.Value);
            else
                throw new Exception($"Could not create an order without a price for symbol {symbol}. Orders must be limit orders at Bittrex");

            lock (ActiveOrders)
            {
                ActiveOrders.Add(order.ClOrdId, order);
                OrderIdMappers.Add(internalClOdId, order.ClOrdId);
            }

            BittrexRestClient.SpotApi.Trading.PlaceOrderAsync(symbol, 
                                                               OrderConverter.ConvertSide(side), 
                                                               OrderType.Limit,
                                                               TimeInForce.GoodTillCanceled, 
                                                               ordQty, 
                                                               price,
                                                               clientOrderId: internalClOdId);
            DoLog($"@{BittrexConfiguration.Name}:New {side} order Order sent to market for symbol {symbol} for qty {ordQty} and price {price}", MessageType.Information);
        }

        private void EvalRouteError(Order order, Exception ex)
        {
            DoLog($"Error routing order for symbol {order.Security.Symbol}: {ex.Message}", MessageType.Error);

            //TODO--> Build EvalRouteError --> Route unexisting symbol
            //GetOrderResponse ordResp = GetTheoreticalResponse(order, "");
            //order.OrdStatus = OrdStatus.Rejected;
            //ordResp.CancelInitiated = true;


            //if (ex.Message.Contains(_INSUFFICIENT_FUNDS))
            //{
            //    order.RejReason = _INSUFFICIENT_FUNDS;
            //}
            //else if (ex.Message.Contains(MIN_TRADE_REQUIREMENT_NOT_MET))
            //{
            //    order.RejReason = MIN_TRADE_REQUIREMENT_NOT_MET;
            //}
            //else
            //    order.RejReason = ex.Message;

            //ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, ordResp);
            //OnMessageRcv(wrapper);
        }

        private BittrexOrder FetchOrder(Order order)
        {

            if (order.OrderId != null)
            {

                WebCallResult<BittrexOrder> orderResp = BittrexRestClient.SpotApi.Trading.GetOrderAsync(order.OrderId).Result;

                if (orderResp.Success)
                {
                    BittrexOrder resp = null;
                    Error error = null;
                    orderResp.GetResultOrError(out resp, out error);

                    if (error != null)
                        throw new Exception(error.Message);

                    if (resp != null)
                        return resp;
                    else
                        throw new Exception($"Could not find order for {order.OrderId}");
                }
                else
                    throw new Exception(orderResp.Error != null ? orderResp.Error.Message : "Unknown error");

            }
            else
                throw new Exception($"Cannot fetch an order that has not been yet accepted in the market!");

        }

        #endregion

        #region Overriden Methods
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
                    
                    AccountBittrexDataManager = new AccountBittrexDataManager(BittrexConfiguration.ConfigConnectionString);

                    OrderIdMappers = new Dictionary<string, string>();

                    //ExecutionReportThread = new Thread(DoEvalExecutionReport);
                    //ExecutionReportThread.Start();

                    //Todo inicializar mundo Bittrex
                    zHFT.OrderRouters.Bittrex.BusinessEntities.AccountBittrexData bittrexData = AccountBittrexDataManager.GetByAccountNumber(BittrexConfiguration.AccountNumber);

                    if (bittrexData == null)
                        throw new Exception(string.Format("No se encontró ninguna configuración de autenticación contra Bittrex de la cuenta {0}", BittrexConfiguration.AccountNumber));

                    BittrexConfiguration.ApiKey = bittrexData.APIKey;
                    BittrexConfiguration.Secret = bittrexData.Secret;

                    BittrexRestClient = new BittrexRestClient(options =>
                    {
                        options.ApiCredentials = new ApiCredentials(BittrexConfiguration.ApiKey, BittrexConfiguration.Secret);
                        options.RequestTimeout = TimeSpan.FromSeconds(60);
                    });

                    BittrexSocketClient = new BittrexSocketClient(options =>
                    {
                        options.ApiCredentials = new ApiCredentials(BittrexConfiguration.ApiKey, BittrexConfiguration.Secret);
                        options.RequestTimeout = TimeSpan.FromSeconds(60);
                    });

                    SubscribeExecutionReports();

                    return true;
                }
                else
                {
                    DoLog(string.Format("@{0}:Error initializing config file " + configFile, BittrexConfiguration.Name), MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Critic error initializing " + configFile + ":" + ex.Message, BittrexConfiguration.Name),MessageType.Error);
                return false;
            }
        }

        protected override CMState CancelOrder(zHFT.Main.Common.Wrappers.Wrapper wrapper)
        {
            string origClOrderId = wrapper.GetField(OrderFields.OrigClOrdID).ToString();
            try
            {
                //New order id
                string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();
               
                lock (ActiveOrders)
                {
                    
                    if (ActiveOrders.ContainsKey(origClOrderId)  )
                    {
                        Order order = ActiveOrders[origClOrderId];
                        RunCancelOrder(order, false);
                    }
                    else
                        throw new Exception($"Could not find an internal ClOrdId for OrigClOrdId {origClOrderId}");
                }
               
                return CMState.BuildSuccess();

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error cancelling order {1}!:{2}", BittrexConfiguration.Name, origClOrderId, ex.Message), MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        protected override void DoLoadConfig(string configFile, List<string> noValueFields)
        {
            BittrexConfiguration = new Configuration().GetConfiguration<Configuration>(configFile, noValueFields);
        }

        protected override BaseConfiguration GetConfig()
        {
            return BittrexConfiguration;
        }

        protected override string GetQuoteCurrency()
        {
            return BittrexConfiguration.QuoteCurrency;
        }

        protected override CMState ProcessSecurityList(zHFT.Main.Common.Wrappers.Wrapper wrapper)
        {
            throw new NotImplementedException();
        }

        protected override zHFT.Main.Common.DTO.CMState RouteNewOrder(zHFT.Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {
                Order order = GetOrder(wrapper);
                try
                {
                    lock (tLock)
                    {
                        RunNewOrder(order, false);
                    }
                }
                catch (Exception ex)
                {
                    EvalRouteError(order, ex);
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }
   
        protected override zHFT.Main.Common.DTO.CMState UpdateOrder(zHFT.Main.Common.Wrappers.Wrapper wrapper)
        {
            string origClOrderId = wrapper.GetField(OrderFields.OrigClOrdID).ToString();

            string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();
            try
            {

                if (wrapper.GetField(OrderFields.OrigClOrdID) == null)
                    throw new Exception("Could not find OrigClOrdID for order updated");

                if (wrapper.GetField(OrderFields.ClOrdID) == null)
                    throw new Exception("Could not find ClOrdId for new order");

                lock (ActiveOrders)
                {
                        
                    if (ActiveOrders.ContainsKey(origClOrderId))
                    {
                        Order order = ActiveOrders[origClOrderId];

                        if (order != null)
                        {
                            if (order.OrderId == null)
                                throw new Exception($"The client order id {origClOrderId} has not been accepted on the exchange and cannot be updated ");

                            //Fetch Order --> w/rest client
                            BittrexOrder ordResp = FetchOrder(order);

                            //Cancel the order
                            RunCancelOrder(order, true);

                            Thread.Sleep(100);

                            //Damos el alta
                            double? newPrice = (double?)wrapper.GetField(OrderFields.Price);
                            order.Price = newPrice;
                            order.ClOrdId = clOrderId;
                            decimal lvsQty = 0;

                            if (ordResp.Quantity.HasValue)
                                lvsQty = ordResp.Quantity.Value - ordResp.QuantityFilled;
                            else
                                throw new Exception($"Could not find the market order quantity to replace!");

                            order.OrderQty = Convert.ToDouble(lvsQty);//Nos aseguramos de solo rutear la nueva cantidad
                            try
                            {
                                RunNewOrder(order, true);
                            }
                            catch (Exception ex)
                            {
                                if (ActiveOrders.ContainsKey(order.OrderId))
                                    ActiveOrders.Remove(order.OrderId);
                                EvalRouteError(order, ex);
                            }
                        }
                    }
                    else
                        throw new Exception(string.Format("Could not find an active order to cancel (and then update) for Client Order Id {0}", origClOrderId));

                  
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error updating order {1}!:{2}", BittrexConfiguration.Name, origClOrderId, ex.Message), MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        protected override bool RunCancelOrder(Order order, bool update)
        {
            if (order.OrderId != null)
            {
                BittrexRestClient.SpotApi.Trading.CancelOrderAsync(order.OrderId);
                return true;
            }
            else
                return false;
        }

        #endregion
    }
}
