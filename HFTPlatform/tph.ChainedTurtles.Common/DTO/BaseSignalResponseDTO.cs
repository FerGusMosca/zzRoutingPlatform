using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{
    public class BaseSignalResponseDTO
    {
        #region Public Static Consts

        public static string _FLAT_ACTION = "FLAT";

        public static string _MOCK_STRATEGY = "MOCK_STRATEGY";

        #endregion

        #region Public Attributes

        public string strategy { get; set; }

        public long timestamp { get; set; }


        public string action { get; set; }//LONG,SHORT,FLAT

        #endregion

    }
}
