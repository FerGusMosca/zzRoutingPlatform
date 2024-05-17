using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace tph.MultipleLegsMarketClient.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public List<MarketClient> MarketCLients { get; set; }

        public List<PublishClient> PublishClients { get; set; }

        #endregion

        #region Public Overriden Methods
        public override bool CheckDefaults(List<string> result)
        {
            return true;
        }

        #endregion
    }
}
