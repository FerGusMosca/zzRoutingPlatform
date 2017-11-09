using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;

namespace zHFT.MarketClient.Quickfix.Common.Configuration
{
    public class Configuration : BaseConfiguration,IConfiguration
    {
        #region Public Attributes

        public bool Active { get; set; }

        public string FIXConfigFile { get; set; }

        #endregion

        #region Private Methods

        public override  bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (string.IsNullOrEmpty(Name))
            {
                result.Add("Name");
                resultado = false;
            }

            if (string.IsNullOrEmpty(FIXConfigFile))
            {
                result.Add("FIXConfigFile");
                resultado = false;
            }

            return resultado;
        }

        #endregion

    }
}
