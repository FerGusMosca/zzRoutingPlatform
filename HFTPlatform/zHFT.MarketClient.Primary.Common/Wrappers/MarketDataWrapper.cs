using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.MarketClient.Primary.Common.Converters;

namespace zHFT.MarketClient.Primary.Common.Wrappers
{
    public class MarketDataWrapper : zHFT.MarketClient.Common.Wrappers.MarketDataWrapper
    {

        #region Protected Attributes

        protected string Market { get; set; }

        #endregion

        #region Constructors

        public MarketDataWrapper(Security pSecurity,string pMarket, IConfiguration pConfig):base(pSecurity,pConfig)
        {
            Market = pMarket;
        }

        #endregion

        #region Public Methods
        public override object GetField(Main.Common.Enums.Fields field)
        {
            MarketDataFields mdField = (MarketDataFields)field;

            if (mdField == MarketDataFields.Symbol)
                return SecurityConverter.GetFullSymbolFromCleanSymbol(base.Security.Symbol, Market);
            else
                return base.GetField(field);
        }

        #endregion
    }
}
