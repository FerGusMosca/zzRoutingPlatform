using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{
    public class DataBaseConnectionConfigDTO
    {
        #region Public Attributes

        public string connectionString { get; set; }

        public int refreshEveryNMinutes { get; set; }

        #endregion
    }
}
