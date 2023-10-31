using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.StrategyHandlers.Common.Converters
{
    public class PositionConverter : ConverterBase
    {
        #region Protected Attributs


        #endregion

        #region Constructors

        public PositionConverter()
        {
        }

        #endregion

        #region Private Methods

        private void RunMainValidations(Wrapper wrapper)
        {
            if (wrapper.GetAction() != Actions.NEW_POSITION && wrapper.GetAction()!=Actions.NEW_POSITION_CANCELED)
               throw new Exception("Invalid action building position");
        
        }

        private  Security BuildSecurity(Wrapper wrapper)
        {
            Security sec = new Security();
            sec.Symbol = (ValidateField(wrapper, PositionFields.Symbol) ? Convert.ToString(wrapper.GetField(PositionFields.Symbol)) : null);

            return sec;
        }

        #endregion

        #region Public Methods
        public Position GetPosition(Wrapper wrapper, IConfiguration pConfig)
        {
            Config = pConfig;
            RunMainValidations(wrapper);
            ValidatePosition(wrapper);
            Position pos = new Position();
            pos.Security = BuildSecurity(wrapper);
            pos.LoadPosId((ValidateField(wrapper, PositionFields.PosId) ? Convert.ToString(wrapper.GetField(PositionFields.PosId)) : null));
            pos.Side = (ValidateField(wrapper, PositionFields.Side) ? (Side) wrapper.GetField(PositionFields.Side) : Side.Unknown);
            pos.PosStatus = (ValidateField(wrapper, PositionFields.PosStatus) ? (PositionStatus)wrapper.GetField(PositionFields.PosStatus) : PositionStatus.Unknown);
            pos.Exchange =(ValidateField(wrapper, PositionFields.Exchange) ? Convert.ToString( wrapper.GetField(PositionFields.Exchange)) : null);
            pos.QuantityType = (ValidateField(wrapper, PositionFields.QuantityType) ? (QuantityType) wrapper.GetField(PositionFields.QuantityType) : QuantityType.OTHER);
            pos.PriceType = (ValidateField(wrapper, PositionFields.PriceType) ? (PriceType) wrapper.GetField(PositionFields.PriceType) : PriceType.FixedAmount);
            pos.Qty = (ValidateField(wrapper, PositionFields.Qty) ? (double?) Convert.ToDouble(wrapper.GetField(PositionFields.Qty)) : null);
            pos.CashQty = (ValidateField(wrapper, PositionFields.CashQty) ? (double?) Convert.ToDouble(wrapper.GetField(PositionFields.CashQty)) : null);
            pos.Percent = (ValidateField(wrapper, PositionFields.Percent) ? (double?) Convert.ToDouble(wrapper.GetField(PositionFields.Percent)) : null);
            pos.ExecutionReports.AddRange((ValidateField(wrapper, PositionFields.ExecutionReports) ? (IList<ExecutionReport>)wrapper.GetField(PositionFields.ExecutionReports) : new List<ExecutionReport>()));
            pos.Orders.AddRange((ValidateField(wrapper, PositionFields.Orders) ? (IList<Order>)wrapper.GetField(PositionFields.Orders) : new List<Order>()));

            if (wrapper.GetAction() == Actions.NEW_POSITION)
            {
                pos.NewDomFlag = false;
                pos.PositionCleared = false;
                pos.PositionCanceledOrRejected = false;
                pos.PosStatus = PositionStatus.PendingNew;
                pos.NewPosition = true;
            }
            else if (wrapper.GetAction() == Actions.NEW_POSITION_CANCELED)
            {
                pos.NewDomFlag = false;
                pos.PositionCleared = false;
                pos.PositionCanceledOrRejected = true;
                pos.PosStatus = PositionStatus.Rejected;
                pos.PositionRejectReason = (ValidateField(wrapper, PositionFields.PositionRejectReason) ? (PositionRejectReason?)(wrapper.GetField(PositionFields.PositionRejectReason)) : null);
            }
            else
                throw new Exception(string.Format("Could not create positions for unknown action {0}", wrapper.GetAction().ToString()));

            return pos;
        }

        #endregion

    }
}
