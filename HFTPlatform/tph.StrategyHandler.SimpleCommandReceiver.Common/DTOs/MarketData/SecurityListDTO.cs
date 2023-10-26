using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData
{
    public class SecurityListDTO
    {
        #region Public Attributes

        public int SecurityListRequestId { get; set; }

        public SecurityListRequestType SecurityListRequestType { get; set; }

        public List<Security> Securities { get; set; }

        #endregion
    }
}
