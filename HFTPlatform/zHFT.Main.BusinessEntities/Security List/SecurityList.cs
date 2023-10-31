using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.BusinessEntities.Security_List
{
    public class SecurityList
    {
        #region Public Attributes

        public SecurityRequestResult SecurityRequestResult { get; set; }

        public SecurityListRequestType? SecurityListRequestType { get; set; }

        public string MarketID { get; set; }

        public string MarketSegmentID { get; set; }

        public int TotNoRelatedSym { get; set; }

        public string LastFragment { get; set; }

        public int NoRelatedSym { get; set; }

        public List<Security> Securities { get; set; }

        #endregion
    }
}
