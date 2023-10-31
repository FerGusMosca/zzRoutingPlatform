using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.MarketClient.Common.Wrappers;

namespace zHFT.InstructionBasedMarketClient.Bittrex.Common.Wrappers
{
    public class BittrexMarketDataWrapper : MarketDataWrapper
    {
         #region Constructors

        public BittrexMarketDataWrapper(Security pSecurity, IConfiguration pConfig) 
                :base(pSecurity,pConfig)
        {
        }

        #endregion


        #region Public Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {

            MarketDataFields mdField = (MarketDataFields)field;

            if (Security == null)
                return MarketDataFields.NULL;

            else if (mdField == MarketDataFields.Currency)
                return Security.MarketData.Currency;
            else
                return base.GetField(field);
        }


        public override string ToString()
        {
            if (Security != null)
            {
                string resp = string.Format("Symbol={0} ", Security.Symbol);

                if (Security.MarketData != null)
                {
                    resp += string.Format(" LastPrice={0}", Security.MarketData.Trade.HasValue ? Security.MarketData.Trade.Value.ToString("0.########") : "no data");
                    resp += string.Format(" BestBidPrice={0}", Security.MarketData.BestBidPrice.HasValue ? Security.MarketData.BestBidPrice.Value.ToString("0.########") : "no data");
                    resp += string.Format(" BestAskPrice={0}", Security.MarketData.BestAskPrice.HasValue ? Security.MarketData.BestAskPrice.Value.ToString("0.########") : "no data");

                }

                return resp;
            }
            else
                return "";

        }

        #endregion
    }
}
