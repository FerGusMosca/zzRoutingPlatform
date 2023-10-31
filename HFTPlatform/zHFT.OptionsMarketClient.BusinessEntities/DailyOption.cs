using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.OptionsMarketClient.BusinessEntities
{
    public class DailyOption
    {
        #region Pulblic Attributes

        public Option Option { get; set; }

        public DateTime Date { get; set; }

        public bool Processed { get; set; }

        #endregion
    }
}
