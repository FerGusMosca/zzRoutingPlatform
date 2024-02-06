using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.BusinessEntities
{
    public class DateRangeClassification
    {
        #region Public Consts

        public static string _LONG_CLASSIF = "LONG";

        public static string _SHORT_CLASSIF = "SHORT";

        #endregion

        #region Public Attributes

        public string Key { get; set; }

        public DateTime DateStart { get; set; }

        public DateTime? DateEnd { get; set; }

        public string Classification { get; set; }

        #endregion

        #region Public Methods

        public bool IsLongClassif() { return _LONG_CLASSIF == Classification; }

        public bool IsShortClassif() { return _SHORT_CLASSIF == Classification; }

        #endregion
    }
}
