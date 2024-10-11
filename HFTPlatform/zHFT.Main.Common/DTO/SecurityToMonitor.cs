using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.Common.DTO
{
    public  class SecurityToMonitor
    {

        #region Public Consts


        public static string _DEFAULT_SECURITY_TYPE = "CS";

        public static string _DEFAULT_CURRENCY = "USD";

        public static string _DEFAULT_EXCHANGE = "SMART";

        #endregion

        #region Public Attributes


        public string Symbol { get; set; }

        public string SecurityType { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }

        public string MonitoringType { get; set; }

        public List<IndicatorToMonitor> Indicators { get; set; }

        #endregion

        #region Public Methods

        public void LoadDefaults()
        {
            if (string.IsNullOrEmpty(SecurityType))
                SecurityType = _DEFAULT_SECURITY_TYPE;

            if (string.IsNullOrEmpty(Currency))
                Currency = _DEFAULT_CURRENCY;

            if (string.IsNullOrEmpty(Exchange))
                Exchange = _DEFAULT_EXCHANGE;
        
        }

        public bool LoadTrendliens()
        {
            return MonitoringType == zHFT.Main.Common.Enums.MonitoringType.ONLY_TRENDLINE.ToString()
                || MonitoringType == zHFT.Main.Common.Enums.MonitoringType.TRENDLINE_PLUS_ROUTING.ToString();



        }

        public MonitoringType GetMonitoringType()
        {
            if (MonitoringType == zHFT.Main.Common.Enums.MonitoringType.ONLY_ROUTING.ToString())
                return Common.Enums.MonitoringType.ONLY_ROUTING;
            else if (MonitoringType == zHFT.Main.Common.Enums.MonitoringType.ONLY_TRENDLINE.ToString())
                return Common.Enums.MonitoringType.ONLY_TRENDLINE;
            else if (MonitoringType == zHFT.Main.Common.Enums.MonitoringType.TRENDLINE_PLUS_ROUTING.ToString())
                return Common.Enums.MonitoringType.TRENDLINE_PLUS_ROUTING;
            else if (MonitoringType == zHFT.Main.Common.Enums.MonitoringType.ONLY_SIGNAL.ToString())
                return Common.Enums.MonitoringType.ONLY_SIGNAL;
            else throw new Exception($"Could not process a Monitoring Type of value {MonitoringType}");

        }

        #endregion 
    }
}
