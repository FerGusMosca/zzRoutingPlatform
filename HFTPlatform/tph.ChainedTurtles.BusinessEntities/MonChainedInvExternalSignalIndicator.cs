using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.DTO;
using tph.ChainedTurtles.Common.Interfaces;
using tph.ChainedTurtles.Common.Util;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.ChainedTurtles.BusinessEntities
{
    public class MonChainedInvExternalSignalIndicator : MonChainedTurtleIndicator, ITradingEnity
    {

        #region Protected Attributes

        protected List<Security> Securities { get; set; }

        protected IExternalSignalClient SignalClient { get; set; }

        protected bool IsLongSignalTriggered {  get; set; }

        protected bool IsShortSignalTriggered {  get; set; }

        #endregion

        #region Ctor
        public MonChainedInvExternalSignalIndicator(List<Security> pSecurities, TurtlesCustomConfig pTurtlesCustomConfig, string pCode, ILogger pLogger) : base(null, pTurtlesCustomConfig, pCode,pIsSingleSecurityIndicator:false)
        {
            Security = new Security() { Symbol = "<Multiple Symbol Indicators>", SecType = zHFT.Main.Common.Enums.SecurityType.OTH };
            Securities = pSecurities;
            IsSingleSecurityIndicator = true;
            Logger=pLogger;
            LoadConfigValues(pTurtlesCustomConfig.CustomConfig);
        }

        #endregion


        #region Private Methods

        public void OnLogMessage(string msg, Constants.MessageType type)
        { 
            if(Logger!=null)
                Logger.DoLog(msg, type);
        }

        private void LoadConfigValues(string customConfig)
        {
            //
            try
            {
                ExternalSignalTurtleIndicatorConfigDTO resp = JsonConvert.DeserializeObject<ExternalSignalTurtleIndicatorConfigDTO>(customConfig);


                MarketStartTime = TurtleIndicatorBaseConfigLoader.GetMarketStartTime(resp);
                MarketEndTime = TurtleIndicatorBaseConfigLoader.GetMarketEndTime(resp);
                ClosingTime = TurtleIndicatorBaseConfigLoader.GetClosingTime(resp);
                RequestHistoricalPrices = TurtleIndicatorBaseConfigLoader.GetRequestHistoricalPrices(resp, true);

                if (RequestHistoricalPrices)
                    HistoricalPricesPeriod = TurtleIndicatorBaseConfigLoader.GetHistoricalPricesPeriod(resp);
                else
                    HistoricalPricesPeriod = 0;

                if (resp.commConfig == null)
                    throw new Exception("config value commConfig does not exist and it is mandatory for an external signal indicator");

                if (string.IsNullOrEmpty(resp.extSignalAssembly))
                    throw new Exception("config value extSignalAssembly does not exist and it is mandatory for an external signal indicator");
                else
                {
                    SignalClient = InstanceBuilder.BuildInsance<IExternalSignalClient>(resp.extSignalAssembly, resp.commConfig, Logger);

                    SignalClient.Connect();
                };
                
            }
            catch (Exception ex)
            {
                throw new Exception($"CRITICAL error deserializing custom config for symbol {Security.Symbol}:{ex.Message} ");
            }
        }

        #endregion


        #region ITradingEnity
        public string GetCandleReferencePrice()
        {
            return CandleReferencePrice;
        }

        public int GetHistoricalPricesPeriod()
        {
            return HistoricalPricesPeriod;
        }


        public override List<Security> GetSecurities()
        {
            return Securities;

        }

        #endregion

        #region Overriden Methods

        public override bool EvalSignalTriggered()
        {
            return IsLongSignalTriggered || IsShortSignalTriggered;
        }

        public override bool LongSignalTriggered()
        {
            try
            {

                TimestampRangeClassificationDTO signal = SignalClient.EvalSignal();
                IsLongSignalTriggered=signal.IsLongSignalTriggered();
                return IsLongSignalTriggered;
            }
            catch (Exception ex)
            {

                string msg = $"CRITICAL ERROR at MonChainedInvExternalSignalIndicator - ShortSignalTriggered : {ex.Message}";
                Logger.DoLog(msg, Constants.MessageType.Error);
                return false;
            }

        }

        public override bool ShortSignalTriggered()
        {
            try
            {
                TimestampRangeClassificationDTO signal = SignalClient.EvalSignal();
                IsShortSignalTriggered=signal.IsShortSignalTriggered();
                return IsShortSignalTriggered;
            }
            catch (Exception ex) {

                string msg = $"CRITICAL ERROR at MonChainedInvExternalSignalIndicator - ShortSignalTriggered : {ex.Message}";
                Logger.DoLog(msg, Constants.MessageType.Error);
                return false;
            }
        }

        public override bool EvalClosingOnTargetPct(PortfolioPosition portfPos)
        {
            return false;//Discarding this option for this kind of ind
        }

        public override bool EvalClosingLongPosition(PortfolioPosition portfPos)
        {
            return true;//This one always closes. Then it depends on the other indicators
        }

        public override bool EvalClosingShortPosition(PortfolioPosition portfPos)
        {
            return true;//This one always closes. Then it depends on the other indicators
        }

        #endregion
    }
}
