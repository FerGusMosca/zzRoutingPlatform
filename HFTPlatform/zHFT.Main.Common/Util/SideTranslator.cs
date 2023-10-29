using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.Common.Util
{
    public class SideTranslator
    {
        #region Private Static Consts

        private static string _SIDE_BUY = "BUY";

        private static string _SIDE_SELL = "SELL";

        #endregion


        #region Public Static Methods


        public static Side TranslateNonMandatorySide(string side)
        {
            if (side == _SIDE_BUY)
                return Side.Buy;
            else if (side == _SIDE_SELL)
                return Side.Sell;
            else
                return Side.Unknown;
        
        
        }

        public static Side TranslateMandatorySide(string strSide)
        {
            Side side =TranslateNonMandatorySide(strSide);

            if (side == Side.Unknown)
                throw new Exception($"Invalid value for side {strSide}");

            return side;
        }

        #endregion
    }
}
