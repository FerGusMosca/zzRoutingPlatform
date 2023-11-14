using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Util
{
    public class DateTimeManager
    {

        #region Protected Attributs

        private static DateTime? CurrentDate { get; set; }

        #endregion

        #region Static Attributes

        public static DateTime Now
        {
            get
            {
                if (!CurrentDate.HasValue)
                    return DateTime.Now;
                else
                    return CurrentDate.Value;
            }

            set 
            {
                CurrentDate = value;
            
            }
        }

        public static DateTime? NullNow
        {
            set {

                if (value.HasValue)
                    Now = value.Value;
            }
        }

        #endregion
    }
}
