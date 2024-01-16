using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;

namespace tph.MarketClient.FRED.Common
{
    public class FREDConfiguration : BaseConfiguration
    {

        public  string APIKey { get; set; }

        #region Abstract Methods
        public override bool CheckDefaults(List<string> result)
        {
            return true;
        }

        #endregion
    }
}
