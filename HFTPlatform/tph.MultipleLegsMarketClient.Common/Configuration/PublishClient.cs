using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.MultipleLegsMarketClient.Common.Configuration
{
    public class PublishClient
    {
        #region Public Attributes

        public string Key { get; set; }

        public string OutgoingConfigPath { get; set; }

        public string OutgoingModule { get; set; }

        #endregion
    }
}
