using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.StrategyHandler.Common.Wrappers
{
    public class SecurityListRequestWrapper : Wrapper
    {
        #region Protected Attributes

        protected SecurityListRequestType SecurityListRequestType { get; set; }

        protected string Symbol { get; set; }

        protected SecurityType SecurityType { get; set; }

        public  string Exchange { get; set; }

        public string Currency { get; set; }

        #endregion

        #region Constructors

        public SecurityListRequestWrapper(SecurityListRequestType type,string symbol)
        {
            SecurityListRequestType = type;

            Symbol = symbol;
        
        }

        public SecurityListRequestWrapper(SecurityListRequestType type, string symbol,SecurityType pSecType, string pExchange, 
                                           string pCurrency)
        {
            SecurityListRequestType = type;

            Symbol = symbol;

            SecurityType=pSecType;

            Currency = pCurrency;

            Exchange= pExchange;

        }

        #endregion

        #region Public Methods
        public override object GetField(Main.Common.Enums.Fields field)
        {
            SecurityListRequestField slrField = (SecurityListRequestField)field;

            if (slrField == SecurityListRequestField.Symbol)
                return Symbol;
            else if (slrField == SecurityListRequestField.SecurityListRequestType)
                return SecurityListRequestType;
            else if (slrField == SecurityListRequestField.SecurityType)
                return SecurityType;
            else if (slrField == SecurityListRequestField.Currency)
                return Currency;
            else if (slrField == SecurityListRequestField.Exchange)
                return Exchange;
            else
                return SecurityListRequestField.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.SECURITY_LIST_REQUEST;
        }

        public override string ToString()
        {
            return $"Security List Req.: SecurityListRequestType={SecurityListRequestType} Symbol={Symbol} Currency={Currency} Exchange={Exchange}";
        }

        #endregion
    }
}
