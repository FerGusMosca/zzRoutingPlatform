using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.StrategyHandler.SecurityListSaver.BusinessEntities
{
    public class Bill : Security
    {
        #region Public Attributes

        public int Id { get; set; }

        public bool Expired { get; set; }

        #endregion
    }
}
