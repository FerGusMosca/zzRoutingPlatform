using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Positions;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.Positions
{
    public class PortfolioDTO : WebSocketMessage
    {

        #region Contructors

        public PortfolioDTO()
        {
            Msg = "PortfolioMsg";
        }

        #endregion


        #region Public Attributes


        public List<Position> SecurityPositions { get; set; }  

        public List<Position> LiquidPositions { get;set; }

        public string AccountNumber { get;set; }

        #endregion
    }
}
