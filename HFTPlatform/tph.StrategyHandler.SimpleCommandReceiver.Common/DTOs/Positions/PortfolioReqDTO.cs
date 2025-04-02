using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.Positions
{
    public class PortfolioReqDTO : WebSocketMessage
    {
        #region Contructors

        public PortfolioReqDTO()
        {
            Msg = "PortfolioRequest";
        }

        #endregion


        #region Public Atributes

        public string AccountNumber { get; set; }

        #endregion
    }
}
