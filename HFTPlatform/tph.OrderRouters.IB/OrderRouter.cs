using IBApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.OrderRouters.IB.Common;
using tph.OrderRouters.IB.Common.Converters;
using tph.OrderRouters.IB.Common.DTO;
using tph.OrderRouters.IB.Common.Wrappers;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using Constants = zHFT.Main.Common.Util.Constants;


namespace tph.OrderRouters.IB
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

            SecurityType type = (SecurityType) wrapper.GetField(OrderFields.SecurityType);
            string exchange = (string)wrapper.GetField(OrderFields.Exchange); 
            string currency = (string)wrapper.GetField(OrderFields.Currency); 
            string fullSymbol = (string)wrapper.GetField(OrderFields.Symbol);     
            
            
            OrderConverter.AssginContractType(contract, type);
            contract.Symbol=SecurityConverter.GetSymbol(type,fullSymbol,CurrencySeparators._SECURITY_SYMBOL_SEP_ORIG);
            contract.Currency=SecurityConverter.GetCurrency(type,null,fullSymbol,CurrencySeparators._SECURITY_SYMBOL_SEP_ORIG);
            contract.Exchange = exchange != null ? exchange : IBConfiguration.Exchange ;;
            contract.PrimaryExch = SecurityConverter.GetPrimaryExchange(type);;

            return contract;
        
        }

        protected Order GetNewOrder(Wrapper wrapper)
        {

            OrdType ordType = (OrdType)wrapper.GetField(OrderFields.OrdType);
            double? price=(double?)wrapper.GetField(OrderFields.Price);
            Side side = (Side)wrapper.GetField(OrderFields.Side);
            TimeInForce tif = wrapper.GetField(OrderFields.TimeInForce)!=null? (TimeInForce)wrapper.GetField(OrderFields.TimeInForce):TimeInForce.Day;
            
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
            DoLog(string.Format("Routing Order Id {0} Symbol={1}  Side={2} Qty={2} Type={3} Price={4}", order.OrderId, contract.Symbol, 
                                order.TotalQuantity,order.OrderType, order.LmtPrice, order.Action), Constants.MessageType.Information);
            if (wrapper.GetField(OrderFields.ClOrdID) == null)
                throw new Exception("Could not find ClOrdId for new order");

            string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();
            DoLog(string.Format("DBX-new order set for clOrdId {0} at {1}",clOrderId,DateTime.Now),Constants.MessageType.Information);

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
                    DoLog(string.Format("Cancelling Order Id {0}", orderId), Constants.MessageType.Information);
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
                

                if (!cancel)
                {
                    Order order = GetExistingOrder(wrapper);

                    order.OrderId = orderId;
                    DoLog(string.Format("Updating Order Id {0}", order.OrderId), Constants.MessageType.Information);
                    ClientSocket.placeOrder(order.OrderId, contract, order);
                    OrderIdsMapper.Add(clOrderId, orderId);
                }
                else
                {
                    DoLog(string.Format("Cancelling Order Id {0} ", orderId), Constants.MessageType.Information);
                    
                    ClientSocket.cancelOrder(orderId);
                }
                
                
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
                        DoLog(string.Format("Order error for order Id {0}:{1}-{2}", order.OrderId,errorCode,errorMsg), Constants.MessageType.Information);
                        ExecutionReportWrapper wrapper = new ExecutionReportWrapper(dto, order, contract, Config);

                        OnMessageRcv(wrapper);

                    }
                    else
                        throw new Exception(string.Format("Could find order created for id {0}", dto.Id));
                }
            }
            catch (Exception ex)
            {
                DoLog("Error processing order error:" + ex.Message, Constants.MessageType.Error);

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
                Order order = null;
                Contract contract = null;
                if (OrderList.ContainsKey(dto.Id))
                    order = OrderList[dto.Id];
                else
                    throw new Exception($"Could not find order in OrderList dict for id {dto.Id}");
                
                
                if (ContractList.ContainsKey(dto.Id))
                    contract = ContractList[dto.Id];
                else
                    throw new Exception($"Could not find order in ContractList dict for id {dto.Id}");
                
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
                DoLog("Error processing order status:" + ex.Message, Constants.MessageType.Error);
            
            }
        
        }

        #endregion

        #region Public  Methods

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                
                if (wrapper.GetAction() == Actions.NEW_ORDER)
                {
                    DoLog(string.Format("Routing with IB to market for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Constants.MessageType.Information);
                    RouteNewOrder(wrapper);

                }
                else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                {
                    DoLog(string.Format("Updating order with IB  for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Constants.MessageType.Information);
                    UpdateOrder(wrapper,false);

                }
                else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                {
                    DoLog(string.Format("Cancelling order with IB  for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Constants.MessageType.Information);
                    UpdateOrder(wrapper, true);
                }
                else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
                {
                    DoLog(string.Format("@{0}:Cancelling all active orders @ IB", IBConfiguration.Name), Constants.MessageType.Information);
                    CancelAllOrders();
                }
                else
                {
                    DoLog(string.Format("Could not process order routing for action {0} with IB:", wrapper.GetAction().ToString()),
                          Constants.MessageType.Error);
                    return CMState.BuildFail(new Exception(string.Format("Could not process order routing for action {0} with IB:", wrapper.GetAction().ToString())));
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog("Error routing order to market using IB:" +ex.Message, Constants.MessageType.Error);
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

                    ClientSocket = new EClientSocket(this,this);
                    ClientSocket.eConnect(IBConfiguration.IP, IBConfiguration.Port, IBConfiguration.IdIBClient);
                    
                    EReader= new EReader(ClientSocket, this);
                    EReader.Start();
                    
                    ReaderThread = new Thread(ReaderThreadImp){IsBackground = true};
                    
                    ReaderThread.Start();

                    OrderConverter = new OrderConverter();

                    OrderList = new Dictionary<int, Order>();
                    OrderIdsMapper = new Dictionary<string, int>();
                    ContractList = new Dictionary<int, Contract>();

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
