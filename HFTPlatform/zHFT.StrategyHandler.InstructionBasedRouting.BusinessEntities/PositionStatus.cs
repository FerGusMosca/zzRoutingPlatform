using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities
{
    public class PositionStatus
    {
        #region Public Static Consts

        public static char _OFFLINE = 'A';
        public static char _EXECUTED = '2';
        public static char _IN_MARKET = '0';
        public static char _SENT_TO_MARKET = 'Z';
        public static char _SENT_TO_UNWIND = 'U';
        public static char _SENT_TO_UPDATE = 'K';
        public static char _OFFLINE_UNWINDED = 'W';
        public static char _UNWINDED='T';

        public static string _S_OFFLINE = "A";
        public static string _S_EXECUTED = "2";
        public static string _S_IN_MARKET = "0";
        public static string _S_SENT_TO_MARKET = "Z";
        public static string _S_SENT_TO_UNWIND = "U";
        public static string _S_SENT_TO_UPDATE = "K";
        public static string _S_OFFLINE_UNWINDED = "W";
        public static string S_UNWINDED = "T";

        #endregion

        #region Public Attributes

        public int Id { get; set; }

        public char Code { get; set; }

        public string Description { get; set; }

        #endregion

        #region Public Static Methods

        public static PositionStatus GetPositionSentToMarket()
        {
            return new PositionStatus() { Code = _SENT_TO_MARKET, Description = "Enviada al mercado" };
        }

        public static PositionStatus GetUnwindSentToMarket()
        {
            return new PositionStatus() { Code = _SENT_TO_UNWIND, Description = "Enviada la venta al mercado" };
        }

        public static PositionStatus GetNewPositionStatus(bool online)
        {
            if (online)
                return new PositionStatus() { Code = _EXECUTED, Description = "En Mercado" };
            else
                return new PositionStatus() { Code = _OFFLINE, Description = "Offline" };

        }

        public static PositionStatus GetOfflineUnwindedStatus()
        {
            return new PositionStatus() { Code = _OFFLINE_UNWINDED, Description = "Desarmada" };
        }


        #endregion

        #region Public Methods

        public bool IsOnline()
        {
            return Code == _EXECUTED || Code == _IN_MARKET || Code == _SENT_TO_UPDATE;
        }

        public bool IsUnwinded()
        {
            return Code == _UNWINDED;
        
        }

        #endregion
    }
}
