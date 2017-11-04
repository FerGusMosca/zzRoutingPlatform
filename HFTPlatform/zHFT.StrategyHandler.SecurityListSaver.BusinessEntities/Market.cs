using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.SecurityListSaver.BusinessEntities
{
    public class Market
    {
        #region Public Consts

        public static string _DEFAULT_COUNTRY = "USA";

        #endregion

        #region Public Attributes

        public int Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Country { get; set; }

        #endregion
    }
}
