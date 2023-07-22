using Bittrex.Net.Clients;
using Bittrex.Net.Enums;
using CryptoExchange.Net.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.OrderRouter.Bittrex.Common;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Interfaces;
using zHFT.OrderRouters.Bittrex.Common.Configuration;
using zHFT.OrderRouters.Bittrex.Common.Wrappers;
using zHFT.OrderRouters.Bittrex.DataAccessLayer.Managers;
using zHFT.OrderRouters.Cryptos;
using zHFT.StrategyHandler.IBR.Bittrex.BusinessEntities;
using static zHFT.Main.Common.Util.Constants;

namespace tph.OrderRouter.Bittrex
{
    public class OrderRouter : BaseOrderRouter
    {

        #region Protected Attrbuts

        protected Configuration BittrexConfiguration { get; set; }

        protected AccountBittrexDataManager AccountBittrexDataManager { get; set; }

        protected BittrexRestClient BittrexRestClient { get; set; }

        #endregion

        #region Private Methods

        private void RunNewOrder(Order order, bool update)
        {
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

            BittrexRestClient.SpotApi.Trading.PlaceOrderAsync(symbol, 
                                                               OrderConverter.ConvertSide(side), 
                                                               OrderType.Limit,
                                                               TimeInForce.GoodTillCanceled, 
                                                               ordQty, 
                                                               price);
            DoLog($"@{BittrexConfiguration.Name}:New {side} order Order sent to market for symbol {symbol} for qty {ordQty} and price {price}", MessageType.Information);
        }

        protected void EvalRouteError(Order order, Exception ex)
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
            throw new NotImplementedException();
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

        protected override bool RunCancelOrder(Order order, bool update)
        {
            throw new NotImplementedException();
        }

        protected override zHFT.Main.Common.DTO.CMState UpdateOrder(zHFT.Main.Common.Wrappers.Wrapper wrapper)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
