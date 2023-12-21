using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.Common.Configuration;

namespace zHFT.OrderImbSimpleCalculator.Common.Configuration
{

    public enum QuantityMode
    { 
        Cash,
        Contracts
    }

    public class Configuration : BaseConfiguration
    {



        #region Public Attributes

        public string ConnectionString { get; set; }

        [XmlArray]
        [XmlArrayItem(ElementName = "Symbol")]
        public List<string> StocksToMonitor { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }

        public string OrderRouter { get; set; }

        public string OrderRouterConfigFile { get; set; }

        public bool ResetOnPersistance { get; set; }
        
        public bool CancelActiveOrdersOnStart { get; set; }

        public string SaveEvery { get; set; }

        public double? PositionSizeInCash { get; set; }

        public double? PositionSizeInContracts { get; set; }

        public int MaxOpenedPositions { get; set; }

        public decimal StopLossForOpenPositionPct { get; set; }

        public string FeeTypePerTrade { get; set; }

        public double FeeValuePerTrade { get; set; }

        public int? DecimalRounding{ get; set; }

        public string SecurityTypes { get; set; }

        public int ActiveBlocks { get; set; }

        public int BlockSizeInMinutes { get; set; }

        public string MarketStartTime { get; set; }

        public string MarketEndTime { get; set; }
        
        public string ClosingTime { get; set; }
        
        public bool OnlyLong { get; set; }
        
        public string CandleReferencePrice { get; set; }

        public int HistoricalPricesPeriod { get; set; }

        #endregion

        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool res = true;

            //if (StocksToMonitor.Count==0)
            //{
            //    result.Add("StocksToMonitor");
            //    resultado = false;
            //}

            if (string.IsNullOrEmpty(ConnectionString))
            {
                result.Add("ConnectionString");
                res = false;
            }

            //if (string.IsNullOrEmpty(SaveEvery))
            //{
            //    result.Add("SaveEvery");
            //    resultado = false;
            //}

            if (string.IsNullOrEmpty(OrderRouter))
            {
                result.Add("OrderRouter");
                res = false;
            }

            if (string.IsNullOrEmpty(OrderRouter))
            {
                result.Add("OrderRouter");
                res = false;
            }

            if (string.IsNullOrEmpty(Currency))
            {
                //result.Add("Currency");
                //res = false;
                Currency = _DEFAULT_CURRENCY;
            }

            if (string.IsNullOrEmpty(Exchange))
            {
                //result.Add("Exchange");
                //res = false;
                Exchange = _DEFAULT_EXCHANGE;
            }

            if (string.IsNullOrEmpty(FeeTypePerTrade))
            {
                //result.Add("FeeTypePerTrade");
                //res = false;
            }

            if (string.IsNullOrEmpty(SecurityTypes))
            {
                //result.Add("SecurityTypes");
                //res = false;
                SecurityTypes = _DEFAULT_SECURITY_TYPE;
            }
            
            
            if (string.IsNullOrEmpty(ClosingTime))
            {
                result.Add("ClosingTime");
                res = false;
            }


            if (SecurityTypes == SecurityType.FUT.ToString())
            {

            }
            else
            {

                if (!PositionSizeInCash.HasValue)
                {
                    result.Add("PositionSizeInCash");
                    res = false;
                }
            }

            return res;
        }


        #endregion
    }
}
