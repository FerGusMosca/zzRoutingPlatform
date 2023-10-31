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

namespace zHFT.OrderRouters.Common.Wrappers
{
    //public class InvalidNewPositionWrapper:Wrapper
    //{
    //    #region Private Attributes

    //    protected Position Position { get; set; }

    //    protected IConfiguration Config { get; set; }

    //    protected PositionRejectReason Reason { get; set; }

    //    protected string Text { get; set; }

    //    #endregion

    //    #region Constructors

    //    public InvalidNewPositionWrapper(Position pPosition,PositionRejectReason reason, string text, IConfiguration pConfig) 
    //    {
    //        Position = pPosition;

    //        Config = pConfig;

    //        Reason = reason;

    //        Text = text;
    //    }

    //    #endregion

    //    #region Public Methods

    //    public override string ToString()
    //    {
    //        if (Position != null)
    //        {
    //            return "";//TO DO : Desarrollar el método to string
    //        }
    //        else
    //            return "";
    //    }

    //    public override Main.Common.Enums.Actions GetAction()
    //    {
    //        return Actions.NEW_POSITION_CANCELED;
    //    }

    //    public override object GetField(Main.Common.Enums.Fields field)
    //    {
    //        PositionFields pField = (PositionFields)field;

    //        if (Position == null)
    //            return PositionFields.NULL;

    //        if (pField == PositionFields.Symbol)
    //            return Position.Security.Symbol;
    //        else if (pField == PositionFields.PosId)
    //            return Position.PosId;
    //        else if (pField == PositionFields.Exchange)
    //            return Position.Exchange;
    //        else if (pField == PositionFields.QuantityType)
    //            return Position.QuantityType;
    //        else if (pField == PositionFields.PriceType)
    //            return Position.PriceType;
    //        else if (pField == PositionFields.Qty)
    //            return Position.Qty;
    //        else if (pField == PositionFields.CashQty)
    //            return Position.CashQty;
    //        else if (pField == PositionFields.Percent)
    //            return Position.Percent;
    //        else if (pField == PositionFields.ExecutionReports)
    //            return Position.ExecutionReports;
    //        else if (pField == PositionFields.Orders)
    //            return Position.Orders;
    //        else if (pField == PositionFields.Side)
    //            return Position.Side;
    //        else if (pField == PositionFields.PosStatus)
    //            return Position.PosStatus;
    //        else if (pField == PositionFields.PositionRejectReason)
    //            return Reason;
    //        else if (pField == PositionFields.PositionRejectText)
    //            return Text;


    //        return ExecutionReportFields.NULL;
    //    }

    //    #endregion
    //}
}
