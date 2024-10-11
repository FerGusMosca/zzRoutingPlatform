using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{
    public class OnOffTimestampRangeClassificationDTO: TimestampRangeClassificationDTO
    {

        #region Public Static Consts

        public static string _ON_OFF_CLASSIF ="ON_OFF_CLASSIF";

        private static string _ON_CLASSIF = "ON";

        private static string _OFF_CLASSIF = "OFF";

        #endregion

        #region     Public Methods

        public void Validate()
        {
            if (key != _ON_OFF_CLASSIF)
                throw new Exception($"Invalid value for Classficiation key for classification row of id {id}:{key}. It must be {_ON_OFF_CLASSIF}");



            if (DateTime.Compare(TimestampStart, TimestampEnd) > 0)
                throw new Exception($"Datetime {TimestampStart} must be previous to Datetime {TimestampEnd}");

            if (!(Classification == _ON_CLASSIF || Classification == _OFF_CLASSIF))
                throw new Exception($"Invalid value for classification at On/Off timestamp range: {Classification}. Value must be ON or OFF");

        
        }

        public bool IsOn()
        { 
            return Classification == _ON_CLASSIF;
        
        }

        public bool IsOff()
        {
            return Classification == _OFF_CLASSIF;

        }

        public static OnOffTimestampRangeClassificationDTO BuildOffSignal()
        {
            return new OnOffTimestampRangeClassificationDTO()
            {
                key = _ON_OFF_CLASSIF,
                TimestampStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0),

                TimestampEnd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59),
                Classification = _OFF_CLASSIF,

            };
        
        
        }


        public override bool IsLongSignalTriggered()
        {
            return IsOn();//On -Off indicator just turn on/off the signal, either long or short
        }

        public override bool IsShortSignalTriggered()
        {
            return IsOn();//On -Off indicator just turn on/off the signal, either long or short
        }


        #endregion

    }
}
