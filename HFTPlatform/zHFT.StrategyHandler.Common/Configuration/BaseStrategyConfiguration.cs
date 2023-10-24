using System.Collections.Generic;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;

namespace zHFT.StrategyHandler.Common
{
    public class BaseStrategyConfiguration: BaseConfiguration, IConfiguration
    {
        #region Public Attributes
        
        [XmlArray]
        [XmlArrayItem(ElementName = "Symbol")]
        public List<string> StocksToMonitor { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }
        
        public string SecurityTypes { get; set; }
        
        public string OrderRouter { get; set; }
        
        public string OrderRouterConfigFile { get; set; }

        public string OpeningTime { get; set; }
        
        public string ClosingTime { get; set; }
        
        public int? DecimalRounding{ get; set; }
        
        public bool CancelActiveOrdersOnStart { get; set; }
        
        public string Account { get; set; }
        
        public double StopLossForOpenPositionPct { get; set; }
        
        public int MaxOpenedPositions { get; set; }
        
        public double? PositionSizeInCash { get; set; }
        
        public double? PositionSizeInContracts { get; set; }
        
        public string FeeTypePerTrade { get; set; }

        public double FeeValuePerTrade { get; set; }
        
        public bool OnlyLong { get; set; }
        
        public string CandleReferencePrice { get; set; }
        
        #endregion

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (StocksToMonitor.Count==0)
            {
                result.Add("StocksToMonitor");
                resultado = false;
            }

            if (string.IsNullOrEmpty(OrderRouter))
            {
                result.Add("OrderRouter");
                resultado = false;
            }

            if (string.IsNullOrEmpty(OrderRouter))
            {
                result.Add("OrderRouter");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Currency))
            {
                result.Add("Currency");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Exchange))
            {
                result.Add("Exchange");
                resultado = false;
            }

            if (string.IsNullOrEmpty(FeeTypePerTrade))
            {
                result.Add("FeeTypePerTrade");
                resultado = false;
            }

            if (string.IsNullOrEmpty(SecurityTypes))
            {
                result.Add("SecurityTypes");
                resultado = false;
            }
            
            
            if (string.IsNullOrEmpty(ClosingTime))
            {
                result.Add("ClosingTime");
                resultado = false;
            }

            if (SecurityTypes == SecurityType.FUT.ToString())
            {

            }
            else
            {

                if (!PositionSizeInCash.HasValue)
                {
                    result.Add("PositionSizeInCash");
                    resultado = false;
                }
            }

            return resultado;
        }
    }
}