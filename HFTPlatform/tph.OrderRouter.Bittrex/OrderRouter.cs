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
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Bittrex.Common.Configuration;
using zHFT.OrderRouters.Bittrex.DataAccessLayer.Managers;
using zHFT.OrderRouters.Common.Converters;
using zHFT.OrderRouters.Common.Wrappers;
using zHFT.OrderRouters.Cryptos;
using zHFT.StrategyHandler.IBR.Bittrex.BusinessEntities;
using static zHFT.Main.Common.Util.Constants;
using TimeInForce = Bittrex.Net.Enums.TimeInForce;

namespace tph.OrderRouter.Bittrex
{
    public class OrderRouter : BaseOrderRouter
    {

        #region Private Static COnsts

        private static int _MAX_SECONDS_NEW_ORD = 5;

        #endregion

        #region Protected Attrbuts

        protected Configuration BittrexConfiguration { get; set; }

        protected AccountBittrexDataManager AccountBittrexDataManager { get; set; }

        protected BittrexRestClient BittrexRestClient { get; set; }

        protected BittrexSocketClient BittrexSocketClient { get; set; }

        protected Dictionary<string, Order> PendingReplacements { get; set; }

        protected Dictionary<string , DateTime> PendingNews { get; set; }

        protected Dictionary<string, decimal> PrevCumQtys { get; set; }

        protected ExecutionReportConverter ExecutionReportConverter { get; set; }

        protected object tLockUpdate = new object();

        #endregion

        #region Private Methods

        private ExecutionReport EvalFinishedExecutionReport(ExecutionReportWrapper wrapper, DataEvent<BittrexOrderUpdate> data)
        {
            ExecutionReport execRep= ExecutionReportConverter.GetExecutionReport(wrapper, GetConfig());
            
            if(!execRep.IsActiveOrder())
            {
                if(ActiveOrders.ContainsKey(execRep.Order.ClOrdId))
                    ActiveOrders.Remove(execRep.Order.ClOrdId);


                if (OrderIdMappers.ContainsKey(data.Data.Delta.ClientOrderId))
                    OrderIdMappers.Remove(data.Data.Delta.ClientOrderId);
            }

            return execRep;
        
        }

        private GenericExecutionReportWrapper BuildCanceledExecutionReport(Order order,string reason)
        {
            ExecutionReport cxlRep = new ExecutionReport();
            cxlRep.Order = order;
            cxlRep.ExecType = ExecType.Canceled;
            cxlRep.OrdStatus = OrdStatus.Canceled;
            cxlRep.Text = reason;
            cxlRep.CumQty = order.CumQty.HasValue ? Convert.ToDouble(order.CumQty) : 0;
            cxlRep.TransactTime = DateTime.Now;
            cxlRep.LeavesQty = 0;

            GenericExecutionReportWrapper wrapper = new GenericExecutionReportWrapper(cxlRep);

            return wrapper;
        }

        private void PendingNewThread(object parm)
        {

            try
            {

                while (true)
                {
                    lock (tLockUpdate)
                    {
                        List<string> toRemove = new List<string>();
                        List<Wrapper> toSend = new List<Wrapper>();
                        foreach (string clOrdId in PendingNews.Keys)
                        {
                            try
                            {
                                TimeSpan elapsed = DateTime.Now - PendingNews[clOrdId];

                                if (elapsed.TotalSeconds > _MAX_SECONDS_NEW_ORD)
                                {
                                    DoLog($"HIGH WARNING--> Cancelling order {clOrdId} on timeout!", MessageType.Error);

                                    if (ActiveOrders.ContainsKey(clOrdId))
                                    {
                                        Order order = ActiveOrders[clOrdId];
                                        GenericExecutionReportWrapper wrapper = BuildCanceledExecutionReport(order, "Cancelled because of timeout!");
                                        ActiveOrders.Remove(clOrdId);

                                        toSend.Add(wrapper);
                                        Console.Beep();//<DBG>
                                        DoLog($"HIGH WARNING--> Order {clOrdId} marked for cancelation!", MessageType.Error);

                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                DoLog($"Error canceling order {clOrdId}:{ex.Message}", MessageType.Information);
                            }
                        }

                        toRemove.ForEach(x => PendingNews.Remove(x));
                        toSend.ForEach(x => OnMessageRcv(x));
                    }
                    Thread.Sleep(_MAX_SECONDS_NEW_ORD * 1000);
                }

            }
            catch (Exception ex)
            {
                DoLog($"CRITIAL ERROR @PendingNewThread:{ex.Message}", MessageType.Error);
            }
        
        }

        private decimal GetPrevCumQty(string clOrdId)
        {
            if (PrevCumQtys.ContainsKey(clOrdId))
                return PrevCumQtys[clOrdId];
            else
                return 0;
        
        }

        private void ProcessExecutionReport(DataEvent<BittrexOrderUpdate> data)
        {
            try 
            {
                ExecutionReportWrapper wrapper = null;
                lock (tLockUpdate)
                {
                    if (OrderIdMappers.ContainsKey(data.Data.Delta.ClientOrderId))
                    {
                        string clOrdId = OrderIdMappers[data.Data.Delta.ClientOrderId];

                        if (ActiveOrders.ContainsKey(clOrdId))
                        {
                            bool isRepl = PendingReplacements.ContainsKey(clOrdId);

                            Order order = ActiveOrders[clOrdId];
                            wrapper = new ExecutionReportWrapper(order, data.Data, isRepl);
                            ExecutionReport exeRep = EvalFinishedExecutionReport(wrapper, data);
                            DoLog($"Received exec report for order (int Cl Ord Id= {data.Data.Delta.ClientOrderId} ClOrdId={clOrdId} ) for symbol {order.Security.Symbol}--> Status={data.Data.Delta.Status} and Cum.Qty={data.Data.Delta.QuantityFilled} ER Status={exeRep.OrdStatus} CloseTime={data.Data.Delta.CloseTime}", MessageType.Information);
                            order.CumQty = GetPrevCumQty(clOrdId)+ data.Data.Delta.QuantityFilled;
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
                ProcessExecutionReport(data);
            });
        }


        //TODO DBG
        private decimal UpdatePrice(Side side,decimal price)
        {
            //if (side == Side.Buy)
            //    return price - 10;
            //else
            //    return price + 10;

            return price;
        
        }

        private BittrexOrder RunNewOrder(Order order, bool update)
        {
            string internalClOdId = Guid.NewGuid().ToString();
            string symbol = $"{order.Symbol}-{order.Currency}";
            DoLog($"@{BittrexConfiguration.Name}:Creating order Order for symbol {symbol} w/ClOrdId ={order.ClOrdId}", MessageType.Information);
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

            price = UpdatePrice(side, price);

            WebCallResult<BittrexOrder> plOrder =BittrexRestClient.SpotApi.Trading.PlaceOrderAsync(symbol, 
                                                               OrderConverter.ConvertSide(side), 
                                                               OrderType.Limit,
                                                               TimeInForce.GoodTillCanceled, 
                                                               ordQty, 
                                                               price,
                                                               clientOrderId: internalClOdId).Result;
            DoLog($"@{BittrexConfiguration.Name}:New {side} order Order sent to market for symbol {symbol} for qty {ordQty} and price {price}", MessageType.Information);

            if (plOrder.Success)
            {
                return plOrder.Data;

            }
            else
                return null;
        }

        private void EvalRouteError(Order order,string clOrdId, Exception ex)
        {

            if (ActiveOrders.ContainsKey(clOrdId))
                ActiveOrders.Remove(clOrdId);

            if (PrevCumQtys.ContainsKey(clOrdId))
                PrevCumQtys.Remove(clOrdId);

            string msg = $"Error routing order for symbol {order.Security.Symbol}: {ex.Message}";
            DoLog(msg, MessageType.Error);
            Wrapper cxlWrapper = BuildCanceledExecutionReport(order, msg);
            OnMessageRcv(cxlWrapper);

         
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

        private void WaitUntilCancelled(Order order)
        {
            bool canneled = false;
            int i = 0;

            while (!canneled)
            {

                if (i > 100)//10 segs
                    throw new Exception($"CRITICAL ERROR! COULD NOT CANEL ORDER {order.OrderId} FOR SYMBOL {order.Security.Symbol} AFTER TIMEOUT!");

                BittrexOrder execRep = FetchOrder(order);

                if(execRep.Status== OrderStatus.Closed)
                {
                    canneled = true;
                }
                i++;
                Thread.Sleep(100);

            }
        
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
                    PendingReplacements = new Dictionary<string, Order>();
                    PendingNews = new Dictionary<string, DateTime>();
                    PrevCumQtys = new Dictionary<string, decimal>();
                    ExecutionReportConverter = new ExecutionReportConverter();
                    tLockUpdate = new object();

                    AccountBittrexDataManager = new AccountBittrexDataManager(BittrexConfiguration.ConfigConnectionString);

                    OrderIdMappers = new Dictionary<string, string>();

                    PendingNewCancelationsThread = new Thread(PendingNewThread);
                    PendingNewCancelationsThread.Start();

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
                        DoLog($"Running cancelation for ClOrdId {origClOrderId}", MessageType.Information);
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
                    EvalRouteError(order,order.ClOrdId, ex);
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

                if (PendingReplacements.ContainsKey(origClOrderId))
                {
                    return CMState.BuildSuccess();
                }

                if (wrapper.GetField(OrderFields.OrigClOrdID) == null)
                    throw new Exception("Could not find OrigClOrdID for order updated");

                if (wrapper.GetField(OrderFields.ClOrdID) == null)
                    throw new Exception("Could not find ClOrdId for new order");

                lock (tLockUpdate)
                {
                        
                    if (ActiveOrders.ContainsKey(origClOrderId))
                    {
                        Order order = ActiveOrders[origClOrderId];

                        if (order != null)
                        {
                            if (order.OrderId == null)
                            {
                                DoLog($" WARNING - The client order id {origClOrderId} has not been accepted on the exchange and cannot be updated ",MessageType.Information);
                                return CMState.BuildSuccess();
                            }
                            //Fetch Order --> w/rest client
                            BittrexOrder ordResp = FetchOrder(order);

                            if (ordResp.Status == OrderStatus.Closed)
                            {
                                DoLog($"Trying to cancel an already cancelled or filled order: {origClOrderId}", MessageType.Information);
                                return CMState.BuildSuccess();
                            }

                            PendingReplacements.Add(origClOrderId, order);

                            //Cancel the order
                            RunCancelOrder(order, true);

                            WaitUntilCancelled(order);

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
                                //Prev CumQty added with the new order
                                PrevCumQtys.Add(clOrderId, order.CumQty.HasValue ? order.CumQty.Value : 0);
                                BittrexOrder plOrder= RunNewOrder(order, true);

                                if (plOrder == null || plOrder.Status == OrderStatus.Closed)
                                {
                                    string msg = $"WARNING! - Could not insert new order {clOrderId}. The exchange rejected it!  ";
                                    DoLog(msg,MessageType.Information);
                                    Wrapper cxlWrapper =BuildCanceledExecutionReport(order, msg);

                                    if (ActiveOrders.ContainsKey(clOrderId))
                                        ActiveOrders.Remove(clOrderId);
                                    PrevCumQtys.Remove(clOrderId);
                                    OnMessageRcv(cxlWrapper);
                                }
                            }
                            catch (Exception ex)
                            {
                                
                                EvalRouteError(order,clOrderId, ex);
                            }
                        }
                    }
                    else
                        throw new Exception(string.Format("Could not find an active order to cancel (and then) for Client Order Id {0}", origClOrderId));

                  
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
