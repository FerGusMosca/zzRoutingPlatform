using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.CurrencyListMarketClient.Bittrex.BusinessEntities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.CurrencyListMarketClient.Bittrex.Common.Wrappers
{
    public class SecurityWrapper : Wrapper
    {

        #region Protected Attributes

        protected CryptoCurrency CryptoCurrency { get; set; }

        protected string  Exchange{get;set;}

        #endregion

        #region Constructors

        public SecurityWrapper(CryptoCurrency pCryptoCurrency,string pExchange)
        {
            CryptoCurrency = pCryptoCurrency;

            Exchange = pExchange;
        
        }


        #endregion


        #region Public Methods

        public override object GetField(Fields field)
        {
            CryptoCurrencyFields sField = (CryptoCurrencyFields)field;

            if (CryptoCurrency == null)
                return CryptoCurrencyFields.NULL;

            if (sField == CryptoCurrencyFields.Symbol)
                return CryptoCurrency.Currency;
            else if (sField == CryptoCurrencyFields.SecurityDesc)
                return CryptoCurrency.CurrencyLong;
            else if (sField == CryptoCurrencyFields.SecurityType)
                return SecurityType.CC;
            else if (sField == CryptoCurrencyFields.Exchange)
                return Exchange;
            else if (sField == CryptoCurrencyFields.MarketData)
                return null;
            else if (sField == CryptoCurrencyFields.MinConfirmation)
                return CryptoCurrency.MinConfirmation;
            else if (sField == CryptoCurrencyFields.TxFee)
                return CryptoCurrency.TxFee;
            else if (sField == CryptoCurrencyFields.CoinType)
                return CryptoCurrency.CoinType;
            else if (sField == CryptoCurrencyFields.BaseAddress)
                return CryptoCurrency.BaseAddress;
            else if (sField == CryptoCurrencyFields.Notice)
                return CryptoCurrency.Notice;

            return SecurityFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.SECURITY;
        }

        #endregion
    }
}
