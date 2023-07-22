using Bittrex.Net.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace tph.OrderRouter.Bittrex.Common
{
    public class OrderConverter
    {
        #region Public Static Methods

        public static OrderSide ConvertSide(Side side)
        {
            if (side == Side.Buy)
                return OrderSide.Buy;
            else if (side == Side.Sell)
                return OrderSide.Sell;
            else
                throw new Exception($"Side {side} not implemented on Bittrex");

        }

        #endregion
    }
}
