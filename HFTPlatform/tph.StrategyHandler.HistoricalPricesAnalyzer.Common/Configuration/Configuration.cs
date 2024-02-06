using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using tph.StrategyHandler.HistoricalPricesDownloader.Common.Configuration;

namespace tph.StrategyHandler.HistoricalPricesAnalyzer.Common.Configuration
{
    public class Configuration: HistoricalPricesDownloaderConfiguration
    {
        #region Public Attributes

        public string IndicatorName { get; set; }

        public string IndicatorAnalysisClass { get;set; }

        public string OutputConnectionString { get; set; }

        #endregion

        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {

            if (string.IsNullOrEmpty(IndicatorName))
            {
                result.Add("IndicatorName");
                return false;
            }

            if (string.IsNullOrEmpty(IndicatorAnalysisClass))
            {
                result.Add("IndicatorAnalysisClass");
                return false;
            }

            if (string.IsNullOrEmpty(OutputConnectionString))
            {
                result.Add("OutputConnectionString");
                return false;
            }

            return base.CheckDefaults(result);
        }

        #endregion
    }
}
