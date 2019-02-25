using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.BitMex.BE
{
    public enum SecurityType
    {
        FUT,
        SWAP,
        OPT,//Some sort of perpetual options
        INDX,
        OTH
    }

    public class Security
    {
        #region Public Attributes

        public string SecurityId { get; set; }

        public string Symbol { get; set; }

        public string QuoteSymbol { get; set; }

        public SecurityType SecurityType { get; set; }

        public string MaturityMonthYear { get; set; }

        public DateTime? MaturityDate { get; set; }

        public string SecurityAltID { get; set; }

        public double? MinPriceIncrement { get; set; }

        public double? MinPriceIncrementAmmount { get; set; }

        public double? ContractMultiplier { get; set; }

        public double? InitMargin { get; set; }

        public double? MaintMargin { get; set; }

        public double? TakerFee { get; set; }

        public double? MakerFee { get; set; }

        public decimal? LotSize { get; set; }

        #region Swap Fields

        public double? StrikePrice { get; set; }

        #endregion

        #region Market Data

        public double? LastTrade { get; set; }

        #endregion

        #endregion

        #region Public Methods

        public double GetMargin()
        {
            double margin = 0;

            if (TakerFee.HasValue)
                margin += 3 * TakerFee.Value;//2 for Init. Margin, and 1 for Maint.Margin

            if (InitMargin.HasValue)
                margin += InitMargin.Value;

            if (MaintMargin.HasValue)
                margin += MaintMargin.Value;


            return margin;
        }

        #endregion

        #region Private Consts

        private static string _FFCCSX = "FFCCSX";

        private static string _FFWCSX = "FFWCSX";

        private static string _OCECCS = "OCECCS";

        private static string _OPECCS = "OPECCS";

        //Type for indexs

        private static string _MRCXXX = "MRCXXX";

        private static string _MRIXXX = "MRIXXX";

        private static string _MRRXXX = "MRRXXX";

        #endregion

        #region Public Static Methods

        public static SecurityType GetSecurityTypeFromCode(string code)
        {
            if (code == _FFCCSX)
                return SecurityType.FUT;
            else if (code == _FFWCSX)
                return SecurityType.SWAP;
            else if (code == _OCECCS)
                return SecurityType.OPT;
            else if (code == _OPECCS)
                return SecurityType.OPT;
            else if (code == _MRCXXX)
                return SecurityType.INDX;
            else if (code == _MRIXXX)
                return SecurityType.INDX;
            else if (code == _MRRXXX)
                return SecurityType.INDX;
            else
                return SecurityType.OTH;
        }

        #endregion
    }
}
