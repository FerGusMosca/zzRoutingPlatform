using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ManualTrendlinesAdvisorTurtles.Yahoo.Common.DTO
{
    public class Price
    {
        #region Public Attributes
        public string Symbol { get; set; }
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal Maximum { get; set; }
        public decimal Minimum { get; set; }
        public decimal Close { get; set; }
        public decimal AdjClose { get; set; }
        public long Volume { get; set; }
        #endregion
    }
}
