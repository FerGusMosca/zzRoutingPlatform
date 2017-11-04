using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.Common.Configuration;
using zHFT.StrategyHandler.MomentumPortfolios.Common.Enums;

namespace zHFT.StrategyHandler.MomentumPortfolios.Common.Configuration
{
    [XmlRoot("Configuration")]
    public class Configuration : StrategyConfiguration, IConfiguration
    {
        #region Public Attributes

        public string ConnectionString { get; set; }

        public double PortfolioCashSize { get; set; }

        public DateTime? Date { get; set; }
        public int StocksInPortfolio { get; set; }
        public int HoldingMonths { get; set; }
        public Weight? Weight { get; set; }
        public FilterStocks? FilterStocks { get; set; }
        public Ratio? Ratio { get; set; }
        public Side? Side { get; set; }
        public int PositionsToProcess { get; set; }
        
        #endregion

        #region Private Methods

        public bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            resultado = base.CheckDefaults(result);

            if (string.IsNullOrEmpty(Name))
            {
                result.Add("Name");
                resultado = false;
            }


            if (PortfolioCashSize <= 0)
            {
                result.Add("PortfolioCashSize");
                resultado = false;
            }


            if (!Date.HasValue)
            {
                result.Add("Date");
                resultado = false;
            }


            if (StocksInPortfolio<=0)
            {
                result.Add("StocksInPortfolio");
                resultado = false;
            }

            if (HoldingMonths <= 0)
            {
                result.Add("HoldingMonths");
                resultado = false;
            }

            if (!Weight.HasValue)
            {
                result.Add("Weight");
                resultado = false;
            }

            if (!FilterStocks.HasValue)
            {
                result.Add("FilterStocks");
                resultado = false;
            }

            if (!Ratio.HasValue)
            {
                result.Add("Ratio");
                resultado = false;
            }

            if (!Side.HasValue)
            {
                result.Add("Side");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}
