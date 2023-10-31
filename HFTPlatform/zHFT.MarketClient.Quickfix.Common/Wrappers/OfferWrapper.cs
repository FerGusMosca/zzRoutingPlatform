using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.MarketClient.Quickfix.Common.Wrappers
{
    public class OfferWrapper : Wrapper
    {
        #region Private Attributes

        protected QuickFix50.MarketDataIncrementalRefresh IncrementalRefresh { get; set; }

        protected QuickFix50.MarketDataSnapshotFullRefresh FullRefresh { get; set; }

        protected IConfiguration Config { get; set; }

        #endregion

        #region Constructors

        public OfferWrapper(QuickFix50.MarketDataIncrementalRefresh incRefresh, IConfiguration pConfig) 
        {
            IncrementalRefresh = incRefresh;
            if (pConfig is Configuration.Configuration)
                Config = (Configuration.Configuration)pConfig;
        }

        public OfferWrapper(QuickFix50.MarketDataSnapshotFullRefresh fullRefresh, IConfiguration pConfig)
        {
            FullRefresh = fullRefresh;
            if (pConfig is Configuration.Configuration)
                Config = (Configuration.Configuration)pConfig;
        }

        #endregion

        #region Private Methods


        #endregion

        #region Public Methods

        public override string ToString()
        {
            return "";
        }

        public override object GetField(Main.Common.Enums.Fields field)
        {
            OfferFields mdField = (OfferFields)field;

            return Fields.NULL;
        }

        public override Actions GetAction()
        {
            return Actions.OFFER;
        }

        #endregion
    }
}
