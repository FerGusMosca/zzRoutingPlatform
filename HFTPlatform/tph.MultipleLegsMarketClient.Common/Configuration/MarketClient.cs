using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.MultipleLegsMarketClient.Common.Configuration
{
    public class MarketClient
    {
        #region Public Attributes


        public string IncomingConfigPath { get; set; }

        public string IncomingModule { get; set; }

        public string Type { get; set; }

        public string PublishKey { get; set; }


        #endregion
    }
}
