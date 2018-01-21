using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Binance.BusinessEntities;
using zHFT.OrderRouters.Binance.DataAccessLayer.Managers;
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

        protected void DoEvalExecutionReport()
        {
            //TO DO: Impl. evaluación del exec. report
        }

        protected override CMState RouteNewOrder(Wrapper wrapper)
        { 
            //TODO: Implementar salida de orden
            return CMState.BuildSuccess();
        }

        protected override void RunCancelOrder(Order order, bool update)
        {
            //TODO: Implementar cancelación de orden
        }

        protected override CMState UpdateOrder(Wrapper wrapper)
        {
            //TODO: Implementar update de orden
            return CMState.BuildSuccess();
        }

        protected override CMState CancelOrder(Wrapper wrapper)
        {
            //TODO: Implementar cancelación de orden
            return CMState.BuildSuccess();
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
