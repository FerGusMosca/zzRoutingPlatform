using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.MomentumPortfolios.Common.Enums;

namespace zHFT.StrategyHandler.MomentumPortfolios.BusinessEntities
{
   
    public class Configuration
    {
        #region Public Attributes

        #region Fechas
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int FormationMonths { get; set; }
        public int HoldingMomths { get; set; }
        public int SkippingMonths { get; set; }
        #endregion

        public int StocksInPortfolio { get; set; }
        public Weight Weight { get; set; }
        public decimal TxCosts { get; set; }
        public decimal ThresholdBigCap { get; set; }//En PORCENTAJE base 1: Ej --> 30% se pone 0.3
        public decimal ThresholdMediumCap { get; set; }//En PORCENTAJE base 1: Ej --> 50% se pone 0.5
        public FilterStocks FilterStocks { get; set; }
        public Ratio RankingRatio { get; set; }

        public string Country { get; set; }

        #endregion

        #region Public Static Methods

        public static Ratio GetRankingRatio(string ratio)
        {
            if (ratio == Ratio.CAGR.ToString())
                return Ratio.CAGR;
            if (ratio == Ratio.HQM.ToString())
                return Ratio.HQM;
            else
                throw new Exception("Valor inválido de ratio de valorización " + ratio);

        }

        public static FilterStocks GetFilterStocks(string filter)
        {
            if (filter == FilterStocks.B.ToString())
                return FilterStocks.B;
            else if (filter == FilterStocks.ABSM.ToString())
                return FilterStocks.ABSM;
            else if (filter == FilterStocks.A.ToString())
                return FilterStocks.A;
            else if (filter == FilterStocks.M.ToString())
                return FilterStocks.M;
            else if (filter == FilterStocks.SM.ToString())
                return FilterStocks.SM;
            else
                throw new Exception("Valor inválido de fitro de acciones: " + filter);
        }

        public static Weight GetWeight(string weight)
        {
            if (weight == Weight.EW.ToString())
                return Weight.EW;
            else if (weight == Weight.VW.ToString())
                return Weight.VW;
            else
                throw new Exception("Valor inválido de peso: " + weight);
        }

        #endregion
    }
}
