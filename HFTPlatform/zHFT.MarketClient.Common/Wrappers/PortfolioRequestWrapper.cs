using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.MarketClient.Common.Wrappers
{
    public class PortfolioRequestWrapper : Wrapper
    {
        #region Protected Attributes

        public string AccountNumber {  get; set; }

        #endregion

        #region Constructors

        public PortfolioRequestWrapper(string pAccoutnNumber)
        {
            AccountNumber = pAccoutnNumber;
        }

        #endregion

        #region Public Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            PortfolioRequestFields mdField = (PortfolioRequestFields)field;

            if (AccountNumber == null)
                return PortfolioRequestFields.NULL;

            if (mdField == PortfolioRequestFields.AccountNumber)
                return AccountNumber;
            
            return PortfolioRequestFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.PORTFOLIO_REQUEST;
        }

        #endregion
    }
}
