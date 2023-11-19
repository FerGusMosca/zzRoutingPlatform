using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace tph.MarketClient.Mock.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {

        #region Public Attributes

        public string ConnectionString { get; set; }

        public int PacingMarketDataMilliSec { get; set; }

        public int InitialPacingMarketDataMillisec { get; set; }

        public DateTime From { get; set; }

        public DateTime? To { get; set; }

        public bool SyncHistoricalWithMarketData { get; set; }


        #endregion
        public override bool CheckDefaults(List<string> result)
        {
            bool res = true;

            if (string.IsNullOrEmpty(Name))
            {
                result.Add("Name");
                res = false;
            }

            if (string.IsNullOrEmpty(ConnectionString))
            {
                result.Add("ConnectionString");
                res = false;
            }

            if (PacingMarketDataMilliSec <= 0)
            {
                result.Add("PacingMarketDataMilliSec");
                res = false;
            }


            if (InitialPacingMarketDataMillisec < 0)
            {
                result.Add("InitialPacingMarketDataMillisec");
                res = false;
            }

            if (DateTime.Compare(From,DateTime.MinValue) == 0)
            {
                result.Add("From");
                res = false;
            }

            return res;
        }
    }
}
