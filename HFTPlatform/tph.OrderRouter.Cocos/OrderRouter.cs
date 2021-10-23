using System;
using System.Collections.Generic;
using System.Threading;
using tph.OrderRouter.ServiceLayer;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Common;
using zHFT.OrderRouters.Cryptos;

namespace tph.OrderRouter.Cocos
{
    public class OrderRouter: OrderRouterBase,ICommunicationModule
    {
        #region Protected Attributes
        
        protected  object tLock { get; set; }
        
        protected Dictionary<string,Order> ActiveOrders { get; set; }
        
        protected CocosOrderRouterServiceClient CocosOrderRouterServiceClient { get; set; }
        
        protected tph.OrderRouter.Cocos.Common.Configuration CocosConfiguration
        {
            get { return (tph.OrderRouter.Cocos.Common.Configuration)Config; }
            set { Config = value; }
        }
        
        
        #endregion
        
        #region Protected Methods
        protected override void DoLoadConfig(string configFile, List<string> noValueFields)
        {
            Config = new  tph.OrderRouter.Cocos.Common.Configuration().GetConfiguration< tph.OrderRouter.Cocos.Common.Configuration>(configFile, noValueFields);
        }

        protected override CMState ProcessIncoming(Wrapper wrapper)
        {
            throw new NotImplementedException();
        }

        protected override CMState ProcessOutgoing(Wrapper wrapper)
        {
            throw new NotImplementedException();
        }

        protected void TestValidateNewOrder()
        {
            Order order= new Order();
            
            order.Security=new Security();;
            order.Security.Symbol = "GGAL.BUE";
            order.Security.SecType = SecurityType.CS;
            order.Security.Exchange = "BUE";
            

            order.OrderQty = 10;
            order.Price = 100;
            order.TimeInForce = TimeInForce.Day;
            order.Side = Side.Buy;
            
            CocosOrderRouterServiceClient.ValidateNewOrder(order);
        }

        #endregion

        public CMState ProcessMessage(Wrapper wrapper)
        {
            throw new NotImplementedException();
        }

        public  bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    tLock = new object();

                    //OrderConverter = new OrderConverter();

                    ActiveOrders = new Dictionary<string, Order>();

                    CocosOrderRouterServiceClient = new CocosOrderRouterServiceClient(CocosConfiguration.BaseURL,
                                                                                      CocosConfiguration.DNI, 
                                                                                      CocosConfiguration.User, 
                                                                                      CocosConfiguration.Password);

                    TestValidateNewOrder();

                    //new Thread(EvalExecutionReportsThread).Start();

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile,Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message,Constants.MessageType.Error);
                return false;
            }
        }
    }
}