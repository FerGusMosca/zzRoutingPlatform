using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.CurrencyListMarketClient.Bittrex.BusinessEntities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.CurrencyListMarketClient.Bittrex.Common.Wrappers
{
    public class SecurityListWrapper : Wrapper
    {
        #region Private Static Consts

        private static string _MARKET_ID = "BITTREX";

        #endregion

        #region Protected Attributes

        protected IConfiguration Config { get; set; }

        protected List<CryptoCurrency> CryptoCurrencies { get; set; }

        #endregion

        #region Constructors

        public SecurityListWrapper(List<CryptoCurrency> pCryptoCurrencies, IConfiguration pConfig)
        {
            CryptoCurrencies = pCryptoCurrencies;

            Config = pConfig;
        }

        #endregion

        #region Protected Methods

        private List<Wrapper> GetCurrencies()
        {
            List<Wrapper> securitiesWrappers = new List<Wrapper>();

            foreach (CryptoCurrency cc in CryptoCurrencies)
            {
                securitiesWrappers.Add(new SecurityWrapper(cc, _MARKET_ID));
            }

            return securitiesWrappers;
        }


        #endregion

        #region Public Mehods

        public override object GetField(Fields field)
        {
            SecurityListFields slField = (SecurityListFields)field;

            if (slField == SecurityListFields.SecurityRequestResult)
                return SecurityRequestResult.ValidRequest;
            else if (slField == SecurityListFields.SecurityListRequestType)
                return Main.Common.Enums.SecurityListRequestType.AllSecurities;
            else if (slField == SecurityListFields.MarketID)
                return _MARKET_ID;
            else if (slField == SecurityListFields.MarketSegmentID)
                return null;
            else if (slField == SecurityListFields.TotNoRelatedSym)
                return CryptoCurrencies != null ? CryptoCurrencies.Count() : 0;
            else if (slField == SecurityListFields.LastFragment)
                return true;
            else if (slField == SecurityListFields.Securities)
                return GetCurrencies();

            return SecurityListFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.SECURITY_LIST;
        }

        #endregion

        
    }
}
