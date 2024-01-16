using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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

        protected CandleInterval Interval { get; set; }

        public List<EconomicSeriesValue> EconomicSeriesValues { get; set; }

        public bool Success { get; set; }

        public string Error { get; set; }

        #endregion


        #region Constructors

        public EconomicSeriesWrapper(string pSeriesID,DateTime pFrom, DateTime pTo,CandleInterval pInterval,List<EconomicSeriesValue> pEconomicSeriesValue)
        {
            SeriesID = pSeriesID;
            To = pTo;
            From = pFrom;
            Interval = pInterval;
            EconomicSeriesValues = pEconomicSeriesValue;
            Success = true;


        }

        public EconomicSeriesWrapper(string pSeriesID, DateTime pFrom, DateTime pTo, CandleInterval pInterval, bool pSuccess, string pError)
        {
            SeriesID = pSeriesID;
            To = pTo;
            From = pFrom;
            Interval = pInterval;
            EconomicSeriesValues = new List<EconomicSeriesValue>();
            Success = pSuccess;
            Error = pError;


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
            else if (sField == EconomicSeriesField.Interval)
                return Interval;
            else if (sField == EconomicSeriesField.Success)
                return Success;
            else if (sField == EconomicSeriesField.Error)
                return Error;


            return EconomicSeriesRequestField.NULL;
        }

        #endregion
    }
}
