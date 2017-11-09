using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.MarketClient.Primary.Common.Converters;

namespace zHFT.InstructionBasedFullMarketConnectivity.Primary.Common.Wrappers
{
    public class MarketDataWrapper : zHFT.MarketClient.Common.Wrappers.MarketDataWrapper
    {

        #region Constructors

        public MarketDataWrapper(Security pSecurity, IConfiguration pConfig):base(pSecurity,pConfig)
        {
          
        }

        #endregion

        #region Public Methods
        public override object GetField(Main.Common.Enums.Fields field)
        {
            MarketDataFields mdField = (MarketDataFields)field;

            zHFT.InstructionBasedFullMarketConnectivity.Primary.Common.Configuration.Configuration primaryConf = (zHFT.InstructionBasedFullMarketConnectivity.Primary.Common.Configuration.Configuration)base.Config;

            if (mdField == MarketDataFields.Symbol)
                return SecurityConverter.GetFullSymbolFromCleanSymbol(base.Security.Symbol, primaryConf.Market);
            else
                return base.GetField(field);
        }

        #endregion
    }
}
