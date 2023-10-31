using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.CryptocurrencyListSaver.BusinessEntitie;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Converters
{
    public class SecurityListConverter : ConverterBase
    {
        #region Private Methods

        protected void ValidateSecurityList(Wrapper wrapper, BaseConfiguration config)
        {

            if (!ValidateField(wrapper, SecurityListFields.SecurityRequestResult))
                throw new Exception(string.Format("@{0}: Missing Security Request Result", config.Name));

            SecurityRequestResult result = (SecurityRequestResult)wrapper.GetField(SecurityListFields.SecurityRequestResult);

            if (result != SecurityRequestResult.ValidRequest)
                throw new Exception(string.Format("@{0}: Error retrieving security list. Result: {1}", config.Name, result.ToString()));

            if (!ValidateField(wrapper, SecurityListFields.TotNoRelatedSym))
                throw new Exception(string.Format("@{0}: Missing Tot No RelatedSym ", config.Name));

            if (!ValidateField(wrapper, SecurityListFields.LastFragment))
                throw new Exception(string.Format("@{0}: Missing Last Fragment ", config.Name));
        }

        protected void ValidateSecurity(Wrapper wrapper, BaseConfiguration config)
        {

            if (wrapper.GetField(SecurityFields.Symbol) == Fields.NULL)
                throw new Exception(string.Format("@{0}:Missing Security Symbol", config.Name));

            string symbol = (string)wrapper.GetField(SecurityFields.Symbol);

            if (wrapper.GetField(SecurityFields.SecurityDesc) == Fields.NULL)
                throw new Exception(string.Format("@{0}: Missing SecurityDesc ", config.Name));


            SecurityType secType = (SecurityType)wrapper.GetField(SecurityFields.SecurityType);

            if(secType!=SecurityType.CC)
                throw new Exception(string.Format("@{2}:Could not process security type for symbol {0}:{1}", config.Name, symbol, secType.ToString()));

        }

        #endregion

        #region Public Methods

        public List<CryptoCurrency> GetSecurityList(Wrapper wrapper,BaseConfiguration config, OnLogMessage OnLogMsg)
        {
            ValidateSecurityList(wrapper,config);
            
            string marketID = (string)(ValidateField(wrapper, SecurityListFields.MarketID) ? wrapper.GetField(SecurityListFields.MarketID) : null);
            string MarketSegmentID = (string)(ValidateField(wrapper, SecurityListFields.MarketSegmentID) ? wrapper.GetField(SecurityListFields.MarketSegmentID) : null);
            int TotNoRelatedSym = (int)(ValidateField(wrapper, SecurityListFields.TotNoRelatedSym) ? wrapper.GetField(SecurityListFields.TotNoRelatedSym) : null);

            List<CryptoCurrency> cryptoCurrencies = new List<CryptoCurrency>();

            List<Wrapper> currWrappers = (List<Wrapper>)wrapper.GetField(SecurityListFields.Securities);

            if (currWrappers != null)
            {
                foreach (Wrapper currWrapper in currWrappers)
                {
                    try
                    {
                        CryptoCurrency crypto = new CryptoCurrency();

                        ValidateSecurity(currWrapper, config);

                        crypto.Symbol = (string)currWrapper.GetField(SecurityFields.Symbol);
                        crypto.Name = (string)currWrapper.GetField(SecurityFields.SecurityDesc);
                        crypto.IsActive = (ValidateField(currWrapper, SecurityFields.Halted)) ? (bool)currWrapper.GetField(SecurityFields.Halted) : false;
                        crypto.BaseAddress = (string)currWrapper.GetField(CryptoCurrencyFields.BaseAddress);
                        crypto.CoinType = (string)currWrapper.GetField(CryptoCurrencyFields.CoinType);
                        crypto.Notice = (string)currWrapper.GetField(CryptoCurrencyFields.Notice);
                        crypto.Exchange = (string)currWrapper.GetField(CryptoCurrencyFields.Exchange);

                        cryptoCurrencies.Add(crypto);
                        
                    }
                    catch (Exception ex)
                    {
                        OnLogMsg(string.Format("@{0}:Error processing crypto currency: {1}", config.Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                    }
                }
            }

            return cryptoCurrencies;
        }


        #endregion
    }
}
