using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.MomentumPortfolios.BusinessEntities
{

    public enum AlarmType
    {
        IntradayFall,
        DaysWithNoPrices,
        NoTrading80Pct,
        MarketCapTooBig,
        Unknown
        
    }

    public class StockAlarm
    {
        #region Public Attributes
        public int Id { get; set; }
        public Stock Stock { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Description { get; set; }
        public AlarmType Type { get; set; }
        public Portfolio Portfolio { get; set; }
        #endregion

        #region Public Static Methods
        public static AlarmType GetAlarmType(string alarm)
        {
            if (alarm == AlarmType.IntradayFall.ToString())
                return AlarmType.IntradayFall;
            else if (alarm == AlarmType.DaysWithNoPrices.ToString())
                return AlarmType.DaysWithNoPrices;
            else if (alarm == AlarmType.NoTrading80Pct.ToString())
                return AlarmType.NoTrading80Pct;
            else if (alarm == AlarmType.MarketCapTooBig.ToString())
                return AlarmType.MarketCapTooBig;
            else
                return AlarmType.Unknown;
        }

        #endregion
    }
}
