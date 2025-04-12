using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Generic
{
    public class LightSavingPeriod
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int OffsetHours { get; set; }
    }

}
