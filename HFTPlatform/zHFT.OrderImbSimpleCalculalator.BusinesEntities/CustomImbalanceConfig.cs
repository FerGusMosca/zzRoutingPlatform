using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.OrderImbSimpleCalculator.BusinessEntities
{
    public class CustomImbalanceConfig
    {
        #region Public Attributes

        public string Symbol { get; set; }

        public decimal? OpenImbalance { get; set; }

        public  decimal? CloseImbalance { get; set; }

        public int? CloseWindow { get; set; }

        public bool CloseTurtles { get; set; }

        public bool CloseMMov { get; set; }

        public bool CloseOnImbalance { get; set; }

        #endregion
    }
}
