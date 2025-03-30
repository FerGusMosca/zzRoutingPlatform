using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.DTO
{
    public class GenericError
    {
        #region Public Attributes

        public string code { get; set; }

        public string message { get; set; }

        public string path { get; set; }

        

        #endregion

        #region Public Methods

        public override string ToString()
        {
            string message = $"message={this.message} --> code={code}";
            return message;
        }

        #endregion
    }
}
