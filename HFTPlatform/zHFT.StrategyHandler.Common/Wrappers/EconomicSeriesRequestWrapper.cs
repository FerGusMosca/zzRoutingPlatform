using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.StrategyHandler.Common.Wrappers
{
    public class EconomicSeriesRequestWrapper : Wrapper
    {

        #region Constructors

        public EconomicSeriesRequestWrapper(string pSeriesID, DateTime pFrom, DateTime pTo)
        {
            this.SeriesID = pSeriesID;

            this.From = pFrom;
            this.To = pTo;
        
        }

        #endregion

        #region Protectec Attributes

        public string SeriesID { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        #endregion

        #region Public Wrapper Methods
        public override Actions GetAction()
        {
            return Actions.ECONOMIC_SERIES_REQUEST;
        }

        public override object GetField(Fields field)
        {
            
            EconomicSeriesRequestField sField = (EconomicSeriesRequestField)field;


            if (sField == EconomicSeriesRequestField.SeriesID)
                return SeriesID;
            else if (sField == EconomicSeriesRequestField.From)
                return From;
            else if (sField == EconomicSeriesRequestField.To)
                return To;
          

            return EconomicSeriesRequestField.NULL;
        }

        #endregion
    }
}
