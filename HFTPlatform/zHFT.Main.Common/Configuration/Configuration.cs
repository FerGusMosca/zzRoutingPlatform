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

namespace zHFT.Main.Common.Configuration
{
    [Serializable]
    public class Configuration : BaseConfiguration,IConfiguration
    {
        #region Public Attributes

        public string Name { get; set; }

        public string OutgoingConfigPath { get; set; }

        public string IncomingConfigPath { get; set; }

        public string IncomingModule { get; set; }

        public string OutgoingModule { get; set; }

        public bool LogToConsole { get; set; }

        #endregion

        #region Private Methods

        private void LogEvent(Type type,string mssg)
        {
            if (Name != null)
                EventLog.WriteEntry(Name, mssg, System.Diagnostics.EventLogEntryType.Information);
            else
                EventLog.WriteEntry(type.ToString(), mssg, System.Diagnostics.EventLogEntryType.Information);
        }

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (string.IsNullOrEmpty(Name))
            {
                result.Add("Name");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}
