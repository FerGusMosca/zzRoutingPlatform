using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.OptionsContractSaver.BusinessEntities
{
    public enum PutOrCall
    {
        Call = 'C',
        Put = 'P'
    }


    public class Option
    {
        #region Public Attributes

        public long Id { get; set; }

        public string Symbol { get; set; }

        public string SymbolSfx { get; set; }

        public int StrikeMultiplier { get; set; }

        public PutOrCall PutOrCall { get; set; }

        public decimal StrikePrice { get; set; }

        public string StrikeCurrency { get; set; }

        public string MaturityMonthYear { get; set; }

        public DateTime MaturityDate { get; set; }

        public string Currency { get; set; }

        public string SecurityExchange { get; set; }

        public bool Expired { get; set; }

        #endregion
    }
}
