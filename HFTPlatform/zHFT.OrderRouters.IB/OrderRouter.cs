using IBApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.IB.Common;
using zHFT.OrderRouters.IB.Common.Converters;
using zHFT.OrderRouters.IB.Common.DTO;
using zHFT.OrderRouters.IB.Common.Wrappers;


namespace zHFT.OrderRouters.IB
{
    public class OrderRouter : OrderRouterBase
    {
        #region Private Static Attributes

        private static string _US_PRIMARY_EXCHANGE = "ISLAND";

        private static string _WARNING_INDICATOR = "Warning:";

        #endregion

        #region Protected Attributes

        protected Common.Configuration.Configuration IBConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected EClientSocket ClientSocket { get; set; }

        protected OrderConverter OrderConverter { get; set; }

        protected Dictionary<int, Order> OrderList { get; set; }

        protected Dictionary<string, int> OrderIdsMapper { get; set; }

        protected Dictionary<int, Contract> ContractList { get; set; }

        public static object tLock { get; set; }

        #endregion

        #region Potected Methods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        protected Contract GetContract(Wrapper wrapper)
        { 
            Contract contract = new Contract();

            contract.Symbol = (string)wrapper.GetField(OrderFields.Symbol);

            SecurityType type = (SecurityType) wrapper.GetField(OrderFields.SecurityType);

            OrderConverter.AssginContractType(contract, type);

            contract.Currency = (string)wrapper.GetField(OrderFields.Currency);
            contract.Exchange = IBConfiguration.Exchange;
            contract.PrimaryExch = _US_PRIMARY_EXCHANGE;

            return contract;
        
        }

        protected Order GetNewOrder(Wrapper wrapper)
        {

            OrdType ordType = (OrdType)wrapper.GetField(OrderFields.OrdType);
            double? price=(double?)wrapper.GetField(OrderFields.Price);
            Side side = (Side)wrapper.GetField(OrderFields.Side);
            TimeInForce tif = (TimeInForce)wrapper.GetField(OrderFields.TimeInForce);
            
            // Create a new Order to specify the type of order to be placed
            Order orderInfo = new Order();

            // The OrderId must be *Unique* for each session
            orderInfo.OrderId = NextOrderId;

            NextOrderId++;

            OrderConverter.AssignOrdType(orderInfo, ordType, price);

            OrderConverter.AssignSide(orderInfo, side);

            OrderConverter.AssignTimeInForce(orderInfo, tif);

            // The total size of the limit order
            orderInfo.TotalQuantity = Convert.ToInt32(wrapper.GetField(OrderFields.OrderQty));

            return orderInfo;

        }

        protected Order GetExistingOrder(Wrapper wrapper)
        {

            OrdType ordType = (OrdType)wrapper.GetField(OrderFields.OrdType);
            double? price = (double?)wrapper.GetField(OrderFields.Price);
            Side side = (Side)wrapper.GetField(OrderFields.Side);
            TimeInForce tif = (TimeInForce)wrapper.GetField(OrderFields.TimeInForce);

            // Create a new Order to specify the type of order to be placed
            Order orderInfo = new Order();

            OrderConverter.AssignOrdType(orderInfo, ordType, price);

            OrderConverter.AssignSide(orderInfo, side);

            OrderConverter.AssignTimeInForce(orderInfo, tif);

            // The total size of the limit order
            orderInfo.TotalQuantity = Convert.ToInt32(wrapper.GetField(OrderFields.OrderQty));

            return orderInfo;
        }

        protected void RouteNewOrder(Wrapper wrapper)
        {
            Contract contract = GetContract(wrapper);

            Order order = GetNewOrder(wrapper);

            ClientSocket.placeOrder(order.OrderId, contract, order);
            DoLog(string.Format("Routing Order Id {0}", order.OrderId), Main.Common.Util.Constants.MessageType.Information);
            if (wrapper.GetField(OrderFields.ClOrdID) == null)
                throw new Exception("Could not find ClOrdId for new order");

            string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();

            OrderIdsMapper.Add(clOrderId, order.OrderId);
            OrderList.Add(order.OrderId, order);
            ContractList.Add(order.OrderId, contract);
        }

        protected void CancelAllOrders()
        {
            lock (tLock)
            {
                foreach (int orderId in OrderIdsMapper.Values)
                {
                    DoLog(string.Format("Cancelling Order Id {0}", orderId), Main.Common.Util.Constants.MessageType.Information);
                    ClientSocket.cancelOrder(orderId);
                }
            }
        
        }

        protected void UpdateOrder(Wrapper wrapper,bool cancel)
        {
            Contract contract = GetContract(wrapper);

            if (wrapper.GetField(OrderFields.OrigClOrdID) == null)
                throw new Exception("Could not find OrigClOrdID for order updated");

            if (wrapper.GetField(OrderFields.ClOrdID) == null)
                throw new Exception("Could not find ClOrdId for new order");

            string origClOrderId = wrapper.GetField(OrderFields.OrigClOrdID).ToString();
            //New order id
            string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();

            if (OrderIdsMapper.ContainsKey(origClOrderId))
            {
                int orderId = OrderIdsMapper[origClOrderId];
                Order order = GetExistingOrder(wrapper);
                
                order.OrderId = orderId;

                if (!cancel)
                {
                    DoLog(string.Format("Updating Order Id {0}", order.OrderId), Main.Common.Util.Constants.MessageType.Information);
                    ClientSocket.placeOrder(order.OrderId, contract, order);
                }
                else
                {
                    DoLog(string.Format("Cancelling Order Id {0}", order.OrderId), Main.Common.Util.Constants.MessageType.Information);
                    ClientSocket.cancelOrder(order.OrderId);
                }
                
                OrderIdsMapper.Add(clOrderId, orderId);
            }
        
        }

        protected override void ProcessOrderError(int id, int errorCode, string errorMsg)
        {
            try
            {
                if (errorMsg.Contains(_WARNING_INDICATOR))
                    return;
                
                if (OrderList.ContainsKey(id) && ContractList.ContainsKey(id))
                {
                    Order order = OrderList[id];
                    Contract contract = ContractList[id];

                    OrderStatusDTO dto = new OrderStatusDTO()
                    {
                        Id = id,
                        Status = OrderStatusDTO.GetStatusByErrorCode(errorCode),
                        ErrorMsg = errorMsg

                    };

                    if (order != null)
                    {
                        DoLog(string.Format("Order error for order Id {0}:{1}-{2}", order.OrderId,errorCode,errorMsg), Main.Common.Util.Constants.MessageType.Information);
                        ExecutionReportWrapper wrapper = new ExecutionReportWrapper(dto, order, contract, Config);

                        OnMessageRcv(wrapper);

                    }
                    else
                        throw new Exception(string.Format("Could find order created for id {0}", dto.Id));
                }
            }
            catch (Exception ex)
            {
                DoLog("Error processing order error:" + ex.Message, Main.Common.Util.Constants.MessageType.Error);

            }
        
        }

        protected string GetLatestCLOrdId(int orderId)
        {
            string lastClOrdId = null;
            foreach (string clOrdId in OrderIdsMapper.Keys)
            {
                if (OrderIdsMapper[clOrdId] == orderId)
                    lastClOrdId = string.Copy(clOrdId);
            
            }

            return lastClOrdId;
        
        }

        protected override void ProcessOrderStatus(OrderStatusDTO dto)
        {
            try
            {
                Order order = OrderList[dto.Id];
                Contract contract = ContractList[dto.Id];
                if (order != null)
                {

                    ExecutionReportWrapper wrapper = new ExecutionReportWrapper(dto, order, contract, Config);
                    dto.ClOrdId = GetLatestCLOrdId(dto.Id);

                    OnMessageRcv(wrapper);

                }
                else 
                    throw new Exception(string.Format("Could find order created for id {0}", dto.Id));
            }
            catch (Exception ex)
            {
                DoLog("Error processing order status:" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
            
            }
        
        }

        #endregion

        #region Public  Methods

        public override CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {
                
                if (wrapper.GetAction() == Actions.NEW_ORDER)
                {
                    DoLog(string.Format("Routing with IB to market for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    RouteNewOrder(wrapper);

                }
                else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                {
                    DoLog(string.Format("Updating order with IB  for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    UpdateOrder(wrapper,false);

                }
                else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                {
                    DoLog(string.Format("Canceling order with IB  for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    UpdateOrder(wrapper, true);
                }
                else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
                {
                    DoLog(string.Format("@{0}:Cancelling all active orders @ IB", IBConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                    CancelAllOrders();
                }
                else
                {
                    DoLog(string.Format("Could not process order routing for action {0} with IB:", wrapper.GetAction().ToString()),
                          Main.Common.Util.Constants.MessageType.Error);
                    return CMState.BuildFail(new Exception(string.Format("Could not process order routing for action {0} with IB:", wrapper.GetAction().ToString())));
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog("Error routing order to market using IB:" +ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

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

                    ClientSocket = new EClientSocket(this);
                    ClientSocket.eConnect(IBConfiguration.IP, IBConfiguration.Port, IBConfiguration.IdIBClient);

                    OrderConverter = new OrderConverter();

                    OrderList = new Dictionary<int, Order>();
                    OrderIdsMapper = new Dictionary<string, int>();
                    ContractList = new Dictionary<int, Contract>();

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        protected override CMState ProcessIncoming(Wrapper wrapper)
        {
            //Este Communication Module no tiene modulos de Incoming o Outgoing
            return CMState.BuildFail(new Exception("No incoming module set for IB order router!"));
        }

        protected override CMState ProcessOutgoing(Wrapper wrapper)
        {
            //Este Communication Module no tiene modulos de Incoming o Outgoing
            return CMState.BuildFail(new Exception("No outgoing module set for IB order router!"));
        }

        #endregion
    }
}
