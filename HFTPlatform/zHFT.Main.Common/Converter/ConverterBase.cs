using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.Main.Common.Converter
{
    public class ConverterBase
    {

        #region Protected Attributes

        protected IConfiguration Config { get; set; }

        #endregion

        #region Protected Methods
        
        protected static bool ValidateField(Wrapper wrapper, Fields field)
        {
            return  wrapper.GetField(field) != Fields.NULL;
        }
        
        protected  void ValidateNewOrder(Wrapper wrapper)
        {
            if (!ValidateField(wrapper, OrderFields.Account))
                throw new Exception("Missing account for order");

            if (!ValidateField(wrapper, OrderFields.OrdType))
                throw new Exception("Missing order type for order");

            if (!ValidateField(wrapper, OrderFields.Side))
                throw new Exception("Missing side for order");

            if (!ValidateField(wrapper, OrderFields.Symbol))
                throw new Exception("Missing symbol for order");

            //if (!ValidateField(wrapper, OrderFields.Exchange))
            //    throw new Exception("Missing exchange for order");

            if (!ValidateField(wrapper, OrderFields.SecurityType))
                throw new Exception("Missing Security Type for order");

            OrdType ordType = (OrdType)wrapper.GetField(OrderFields.OrdType);

            //if (ordType != OrdType.Limit)
            //    throw new Exception(string.Format("Tipo de orden no soportado por el ruteador:{0}", ordType));
        
        }


        protected void ValidatePosition(Wrapper wrapper)
        {
            if (!ValidateField(wrapper, PositionFields.PosId))
                throw new Exception("Missing position id");

            if (!ValidateField(wrapper, PositionFields.Symbol))
                throw new Exception("Missing position symbol");

            if (!ValidateField(wrapper, PositionFields.QuantityType))
                throw new Exception("Missing position quantity type");

            if (!ValidateField(wrapper, PositionFields.Side))
                throw new Exception("Missing position side");

            QuantityType qt = (QuantityType)wrapper.GetField(PositionFields.QuantityType);

            if (qt == QuantityType.SHARES || qt==QuantityType.BONDS || qt == QuantityType.CONTRACTS)
            {
                if (!ValidateField(wrapper, PositionFields.Qty))
                    throw new Exception("Missing position quantity");
            }

            if (qt == QuantityType.CURRENCY)
            {
                if (!ValidateField(wrapper, PositionFields.CashQty))
                    throw new Exception("Missing position cash quantity");

            }

        }
        #endregion
    }
}
