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

namespace tph.ChainedTurtles.BusinessEntities
{
    public class MonChainedInvExternalSignalIndicator : MonChainedTurtleIndicator, ITradingEnity
    {

        #region Protected Attributes

        protected List<Security> Securities { get; set; }

        #endregion

        #region Ctor
        public MonChainedInvExternalSignalIndicator(List<Security> pSecurities, TurtlesCustomConfig pTurtlesCustomConfig, string pCode, ILogger pLogger) : base(null, pTurtlesCustomConfig, pCode,pIsSingleSecurityIndicator:false)
        {
            Security = new Security() { Symbol = "<Multiple Symbol Indicators>", SecType = zHFT.Main.Common.Enums.SecurityType.OTH };
            Securities = pSecurities;
            IsSingleSecurityIndicator = true;
            LoadConfigValues(pTurtlesCustomConfig.CustomConfig);
        }

        #endregion


        #region Private Methods

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
    }
}
