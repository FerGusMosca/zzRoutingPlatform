using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.StrategyHandler.Common.Configuration
{
    public class StrategyConfiguration : BaseConfiguration, IConfiguration
    {
        #region Private Conts

        private static string _REPORT_SAVING_BD = "BD";

        private static string _REPORT_SAVING_EXCEL = "EXCEL";

        private static string _REPORT_SAVING_NONE = "NONE";

        #endregion

        #region Public Methods

        public string OrderRouter { get; set; }

        public string OrderRouterConfigFile { get; set; }

        public string ReportSavingMode { get; set; }

        public string ReportSavingConnectionString { get; set; }
        
        #endregion

        #region Private Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (string.IsNullOrEmpty(Name))
            {
                result.Add("Name");
                resultado = false;
            }

            if (string.IsNullOrEmpty(OrderRouter))
            {
                result.Add("OrderRouter");
                resultado = false;
            }

            if (string.IsNullOrEmpty(OrderRouterConfigFile))
            {
                result.Add("OrderRouterConfigFile");
                resultado = false;
            }

            if (string.IsNullOrEmpty(ReportSavingMode))
            {
                result.Add("Name");
                resultado = false;
            }


            return resultado;
        }

        #endregion

        #region Public Methods

        public bool ReportSavingBD()
        {
            return ReportSavingMode == _REPORT_SAVING_BD;
        }

        public bool ReportSavingExcel()
        {
            return ReportSavingMode == _REPORT_SAVING_EXCEL;
        }

        public bool ReportSavingNone()
        {
            return ReportSavingMode == _REPORT_SAVING_NONE;
        }

        #endregion
    }
}
