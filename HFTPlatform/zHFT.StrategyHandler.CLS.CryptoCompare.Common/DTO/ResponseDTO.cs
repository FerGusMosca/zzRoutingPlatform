using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.CLS.CryptoCompare.Common.DTO
{
    public class ResponseDTO
    {
        #region Public Attributes

        public string Response { get; set; }

        public string Message { get; set; }

        public bool HasWarning { get; set; }

        public int Type { get; set; }

        public object Data { get; set; }

        public object RAW { get; set; }

        public object DISPLAY { get; set; }

        #endregion
    }
}
