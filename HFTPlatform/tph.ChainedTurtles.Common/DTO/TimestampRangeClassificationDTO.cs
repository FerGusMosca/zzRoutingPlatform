using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{
    public abstract class TimestampRangeClassificationDTO
    {
        #region Public Attributes

        public long id { get; set; }

        public string key { get; set; }

        public DateTime TimestampStart { get; set; }

        public DateTime TimestampEnd { get; set; }

        public string Classification { get; set; }

        #endregion

        #region Protected Abstract Methods

        public abstract bool IsLongSignalTriggered();


        public abstract bool IsShortSignalTriggered();


        #endregion
    }
}
