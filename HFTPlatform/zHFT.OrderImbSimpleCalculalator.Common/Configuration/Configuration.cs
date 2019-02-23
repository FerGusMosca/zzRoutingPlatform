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

        public string SaveEvery { get; set; }

        public double? PositionSizeInCash { get; set; }

        public double? PositionSizeInContracts { get; set; }

        public int MaxOpenedPositions { get; set; }

        public int WaitingTimeBeforeOpeningPositions { get; set; }

        public decimal PositionOpeningImbalanceThreshold { get; set; }

        public decimal PositionOpeningImbalanceMinThreshold { get; set; }

        public decimal PositionOpeningImbalanceMaxThreshold { get; set; }

        public decimal StopLossForOpenPositionPct { get; set; }

        public string FeeTypePerTrade { get; set; }

        public double FeeValuePerTrade { get; set; }

        public int DecimalRounding{ get; set; }

        public string SecurityTypes { get; set; }

        public int ResetEveryNMinutes { get; set; }

        public string QuantityMode { get; set; }

        #region Futures

        public int? Leverage { get; set; }

        public double? ContractSize { get; set; }

        public double? Margin { get; set; }

        #endregion

        #endregion

        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (StocksToMonitor.Count==0)
            {
                result.Add("StocksToMonitor");
                resultado = false;
            }

            if (string.IsNullOrEmpty(ConnectionString))
            {
                result.Add("ConnectionString");
                resultado = false;
            }

            if (string.IsNullOrEmpty(SaveEvery))
            {
                result.Add("SaveEvery");
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


            if (string.IsNullOrEmpty(QuantityMode))
            {
                result.Add("QuantityMode");
                resultado = false;
            }


            if (SecurityTypes == SecurityType.FUT.ToString())
            {
                if (!ContractSize.HasValue)
                {
                    result.Add("ContractSize");
                    resultado = false;
                }

                if (!Margin.HasValue)
                {
                    result.Add("Margin");
                    resultado = false;
                }

                if (!Leverage.HasValue)
                {
                    result.Add("Leverage");
                    resultado = false;
                }

                if (!PositionSizeInContracts.HasValue)
                {
                    result.Add("PositionSizeInContracts");
                    resultado = false;
                }

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


        #endregion
    }
}
