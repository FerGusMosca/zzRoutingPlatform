using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.BusinessEntities
{
    public class PositionStatus
    {
        #region Public Static COnsts

        public static char _OFFLINE = 'A';
        public static char _EXECUTED = '2';
        public static char _IN_MARKET = '0';

        public static string _S_OFFLINE = "A";
        public static string _S_EXECUTED = "2";
        public static char _S_IN_MARKET = '0';

        #endregion

        #region Public Attributes

        public int Id { get; set; }

        public char Code { get; set; }

        public string Description { get; set; }

        #endregion

        #region Public Static Methods

        public static PositionStatus GetNewPositionStatus(bool online)
        {
            if (online)
                return new PositionStatus() { Code = _EXECUTED, Description = "En Mercado" };
            else
                return new PositionStatus() { Code = _OFFLINE, Description = "Offline" };

        }


        #endregion

        #region Public Methods

        public bool IsOnline()
        {
            return Code == _EXECUTED || Code == _IN_MARKET;
        }

        #endregion
    }
}
