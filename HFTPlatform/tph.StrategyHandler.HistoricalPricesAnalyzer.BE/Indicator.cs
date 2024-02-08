using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.StrategyHandler.HistoricalPricesAnalyzer.BE
{
    public class Indicator : MonTurtlePosition
    {
        #region Protected Attributes

        protected string IndicatorClassifKey { get; set; }

        protected List<DateRangeClassification> DateRangeClassifications { get; set; }

        protected DateRangeClassification LastOpenedClassification { get; set; }

        #endregion

        #region Protected Methods

        public string OpenConigFile(string pConfigFile)
        {
            try {

                string jsonContent = System.IO.File.ReadAllText(pConfigFile);
                return jsonContent;


            }
            catch (Exception ex) {

                throw new Exception($"Could not open config file {pConfigFile}:{ex.Message}");
            
            }
        
        }

        protected T LoadConfigDTO<T>(string jsonContent)
        {
            T config = JsonConvert.DeserializeObject<T>(jsonContent);

            return config;
        }

        #endregion

        #region Public Methods

        public List<DateRangeClassification> GetDateRangeClassifications()
        {
            return DateRangeClassifications;        
        
        }

        public void CloseLastOpenClassification(MarketData md)
        {
            LastOpenedClassification.DateEnd = md.GetReferenceDateTime().Value;
        
        }

        #endregion
    }
}
