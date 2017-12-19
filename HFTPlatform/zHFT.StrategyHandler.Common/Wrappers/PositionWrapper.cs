using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.StrategyHandler.Common.Wrappers
{
    public class PositionWrapper : Wrapper
    {
        #region Protected Attributes

        protected Position Position { get; set; }

        protected IConfiguration Config { get; set; }

        #endregion

        #region Constructors

        public PositionWrapper(Position pPosition, IConfiguration pConfig) 
        {
            Position = pPosition;

            Config = pConfig;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            if (Position != null)
            {
                return "";//TO DO : Desarrollar el método to string
            }
            else
                return "";
        }


        public override object GetField(Main.Common.Enums.Fields field)
        {
            PositionFields pField = (PositionFields)field;

            if (Position == null)
                return PositionFields.NULL;

            if (pField == PositionFields.Symbol)
                return Position.Security.Symbol;
            else if (pField == PositionFields.PosId)
                return Position.PosId;
            else if (pField == PositionFields.Exchange)
                return Position.Exchange;
            else if (pField == PositionFields.QuantityType)
                return Position.QuantityType;
            else if (pField == PositionFields.PriceType)
                return Position.PriceType;
            else if (pField == PositionFields.Qty)
                return Position.Qty;
            else if (pField == PositionFields.CashQty)
                return Position.CashQty;
            else if (pField == PositionFields.Percent)
                return Position.Percent;
            else if (pField == PositionFields.ExecutionReports)
                return Position.ExecutionReports;
            else if (pField == PositionFields.Orders)
                return Position.Orders;
            else if (pField == PositionFields.Side)
                return Position.Side;
            else if (pField == PositionFields.PosStatus)
                return Position.PosStatus;
            else if (pField == PositionFields.Security)
                return new SecurityWrapper(Position.Security, Config);
            else if (pField == PositionFields.Currency)
                return Position.Security.Currency;
            else if (pField == PositionFields.SecurityType)
                return Position.Security.SecType;
            else if (pField == PositionFields.Account)
                return Position.AccountId;


            return ExecutionReportFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.NEW_POSITION;
        }

        #endregion
    }
}
