using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.MarketClient.Common.Wrappers
{
    public class PortfolioWrapper : Wrapper
    {

        #region Constructors

        public PortfolioWrapper(List<Position> pSecurityPositions,List<Position> pLiquidPositions,string pAccountNumber) { 
        
            AccountNumber = pAccountNumber;
            SecurityPositions = pSecurityPositions;
            LiquidPositions = pLiquidPositions;
        }

        #endregion


        #region Protected Attributes

        protected string AccountNumber { get;set; }

        protected List<Position> SecurityPositions { get; set; }

        protected List<Position> LiquidPositions { get; set; }

        public override Actions GetAction()
        {
            return Actions.PORTFOLIO;
        }

        public override object GetField(Fields field)
        {
            PortfolioFields mdField = (PortfolioFields)field;


            if (mdField == PortfolioFields.SecurityPositions)
                return SecurityPositions;
            else if (mdField == PortfolioFields.LiquidPositions)
                return LiquidPositions;
            else if (mdField == PortfolioFields.AccountNumber)
                return AccountNumber;

            return PortfolioFields.NULL;
        }

        public override string ToString()
        {
            return $" Account Number = {AccountNumber} Security Positions = {SecurityPositions.Count} Liquid Positions = {LiquidPositions.Count}";
        }

        #endregion
    }
}
