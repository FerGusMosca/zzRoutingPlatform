using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Common;
using zHFT.OrderRouters.InvertirOnline.Common.Converters;

namespace zHFT.OrderRouters.InvertirOnline
{
    public class OrderRouter : zHFT.OrderRouters.InvertirOnline.Common.OrderRouterBase
    {
        #region Protected Attributes

        protected Common.Configuration.Configuration IOLConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected OrderConverter OrderConverter { get; set; }

        //protected Dictionary<int, Order> OrderList { get; set; }

        protected Dictionary<string, int> OrderIdsMapper { get; set; }

        //protected Dictionary<int, Contract> ContractList { get; set; }

        public static object tLock { get; set; }

        #endregion


        #region Potected Methods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        protected void RouteNewOrder(Wrapper wrapper)
        {
            //Contract contract = GetContract(wrapper);

            //Order order = GetNewOrder(wrapper);

            //ClientSocket.placeOrder(order.OrderId, contract, order);
            //DoLog(string.Format("Routing Order Id {0}", order.OrderId), Main.Common.Util.Constants.MessageType.Information);
            //if (wrapper.GetField(OrderFields.ClOrdID) == null)
            //    throw new Exception("Could not find ClOrdId for new order");

            //string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();

            //OrderIdsMapper.Add(clOrderId, order.OrderId);
            //OrderList.Add(order.OrderId, order);
            //ContractList.Add(order.OrderId, contract);

            //TODO: DEV Ruteo de ordenes
        }

        protected void CancelAllOrders()
        { 
            //TODO: DEV CancellAllOrders
        
        }

        protected void UpdateOrder(Wrapper wrapper, bool cancel)
        {
            //TODO: DEV UpdateOrder
        
        }


        #endregion

        #region Public  Methods

        public override CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {

                if (wrapper.GetAction() == Actions.NEW_ORDER)
                {
                    DoLog(string.Format("Routing with Invertir Online to market for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    RouteNewOrder(wrapper);

                }
                else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                {
                    DoLog(string.Format("Updating order with Invertir Online  for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    UpdateOrder(wrapper, false);

                }
                else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                {
                    DoLog(string.Format("Canceling order with Invertir Online  for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    UpdateOrder(wrapper, true);
                }
                else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
                {
                    DoLog(string.Format("@{0}:Cancelling all active orders @ IB", IOLConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                    CancelAllOrders();
                }
                else
                {
                    DoLog(string.Format("Could not process order routing for action {0} with Invertir Online:", wrapper.GetAction().ToString()),
                          Main.Common.Util.Constants.MessageType.Error);
                    return CMState.BuildFail(new Exception(string.Format("Could not process order routing for action {0} with Invertir Online:", wrapper.GetAction().ToString())));
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog("Error routing order to market using Invertir Online:" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
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
                    tLock = new object();


                    OrderConverter = new OrderConverter();

                    //OrderList = new Dictionary<int, Order>();
                    OrderIdsMapper = new Dictionary<string, int>();
                    //ContractList = new Dictionary<int, Contract>();

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
            return CMState.BuildFail(new Exception("No incoming module set for Invertir Online order router!"));
        }

        protected override CMState ProcessOutgoing(Wrapper wrapper)
        {
            //Este Communication Module no tiene modulos de Incoming o Outgoing
            return CMState.BuildFail(new Exception("No outgoing module set for Invertir Online order router!"));
        }

        #endregion
    }
}
