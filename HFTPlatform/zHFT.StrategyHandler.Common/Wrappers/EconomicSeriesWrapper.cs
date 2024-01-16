using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.StrategyHandler.Common.Wrappers
{
    public class EconomicSeriesWrapper : Wrapper
    {

        #region Protected Attributes

        public string SeriesID { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public List<EconomicSeriesValue> EconomicSeriesValues { get; set; }

        #endregion


        #region Constructors

        public EconomicSeriesWrapper(string pSeriesID,DateTime pFrom, DateTime pTo,List<EconomicSeriesValue> pEconomicSeriesValue)
        {
            SeriesID = pSeriesID;
            To = pTo;
            From = pFrom;
            EconomicSeriesValues = pEconomicSeriesValue;


        }


        #endregion

        #region Wrapper Methods
        public override Actions GetAction()
        {
            return Actions.ECONOMIC_SERIES;
        }

        public override object GetField(Fields field)
        {
            EconomicSeriesField sField = (EconomicSeriesField)field;


            if (sField == EconomicSeriesField.SeriesID)
                return SeriesID;
            else if (sField == EconomicSeriesField.From)
                return From;
            else if (sField == EconomicSeriesField.To)
                return To;
            else if (sField == EconomicSeriesField.Values)
                return EconomicSeriesValues;


            return EconomicSeriesRequestField.NULL;
        }

        #endregion
    }
}
