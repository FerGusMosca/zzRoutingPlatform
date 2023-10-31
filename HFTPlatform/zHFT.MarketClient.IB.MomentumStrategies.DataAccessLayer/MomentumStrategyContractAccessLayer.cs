using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Interfaces;
using zHFT.MarketClient.IB.Common.Configuration;
using zHFT.MarketClient.IB.Common.Interfaces;
using zHFT.StrategyHandler.MomentumPortfolios.DataAccessLayer.Managers;
using zHFT.StrategyHandler.MomentumPortfolios.BusinessEntities;
using zHFT.Main.Common.Util;

namespace zHFT.MarketClient.IB.MomentumStrategies.DataAccessLayer
{
    public class MomentumStrategyContractAccessLayer : IContractAccessLayer
    {

        #region Private Static Consts

        private static string _US_PRIMARY_EXCHANGE = "ISLAND";

        #endregion

        #region Private Attributes

        protected zHFT.MarketClient.IB.Common.Configuration.Configuration Config { get; set; }
        protected OnLogMessage OnLogMsg { get; set; }

        #endregion

        #region Private Methods

        protected void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        #endregion

        #region Public Methods

        public IList<Contract> GetContracts(zHFT.MarketClient.IB.Common.Configuration.Configuration Config,
                                            OnLogMessage pOnLogMsg)
        {
            try
            {
                OnLogMsg=pOnLogMsg;

                StrategyManager mgr = new StrategyManager(Config.StockListAccessLayerConnectionString);

                Strategy strategy = null;

                if (Config.IdPortfolio == 0)
                    strategy = mgr.GetLatestStrategy();
                else
                    strategy = mgr.GetStrategyByPortfolio(Config.IdPortfolio);

                IList<Contract> contracts = new List<Contract>();

                Portfolio portf = strategy.Portfolios.FirstOrDefault();

                foreach (Position pos in portf.Positions)
                {
                    if (!Config.OnlyNotProcessed.HasValue || !Config.OnlyNotProcessed.Value)
                    {
                        contracts.Add(new Contract()
                        {
                            Symbol = pos.Stock.Ticker,
                            SecType = "STK",
                            Currency = "USD",
                            Exchange = "SMART"
                        });
                    }
                    else if (Config.OnlyNotProcessed.HasValue && Config.OnlyNotProcessed.Value)
                    {
                        if (!pos.Processed)
                        {
                            contracts.Add(new Contract()
                            {
                                Symbol = pos.Stock.Ticker,
                                SecType = "STK",
                                Currency = "USD",
                                Exchange = "SMART"
                            });
                        }
                    }
                }

                DoLog("Contracts recovered from DB: " + contracts.Count, Constants.MessageType.Information);

                return contracts;
            }
            catch (Exception ex)
            {
                DoLog("Error recovering contracts from DB: " + ex.Message, Constants.MessageType.Information);
                throw;
            }
        }
        #endregion
    }
}
