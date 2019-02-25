using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouters.Bitmex.Common.Wrappers
{
    public class SecurityListRequestWrapper : Wrapper
    {
        #region Protected Attributes

        protected SecurityListRequestType SecurityListRequestType { get; set; }

        protected string Symbol { get; set; }

        #endregion

        #region Constructors

        public SecurityListRequestWrapper(SecurityListRequestType type, string symbol)
        {
            SecurityListRequestType = type;

            Symbol = symbol;

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
            else
                return SecurityListRequestField.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.SECURITY_LIST_REQUEST;
        }

        #endregion
    }
}
