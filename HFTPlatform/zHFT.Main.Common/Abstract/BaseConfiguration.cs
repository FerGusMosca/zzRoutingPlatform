using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;

namespace zHFT.Main.Common.Abstract
{
    public abstract class BaseConfiguration:IConfiguration
    {

        #region Protected Static Consts

        protected static string _DEFAULT_CURRENCY = "USD";

        protected static string _DEFAULT_EXCHANGE = "SMART";

        protected static string _DEFAULT_SECURITY_TYPE = "CS";

        #endregion

        #region Public Attributes

        public string Name { get; set; }

        #endregion

        #region Private Method

        private void LogEvent(Type type, string mssg)
        {
            //if (Name != null)
            //    EventLog.WriteEntry(Name, mssg, System.Diagnostics.EventLogEntryType.Information);
            //else
            //    EventLog.WriteEntry(type.ToString(), mssg, System.Diagnostics.EventLogEntryType.Information);
        }

        #endregion

        #region Abstract Methods

        public abstract bool CheckDefaults(List<string> result);

        #endregion


        #region Public Methods

        public T GetConfiguration<T>(string configFile, List<string> listaErrs)
        {

            LogEvent(this.GetType(), string.Format("Creating Serializer",configFile));
            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
            FileStream myFileStream = null;
            T config;
            try
            {
                try
                {
                    LogEvent(this.GetType(), string.Format("Creating filestream {0}",configFile));
                    myFileStream = new FileStream(configFile, FileMode.Open);
                    LogEvent(this.GetType(), string.Format("Deserializing Config {0}",configFile));
                    config = (T)mySerializer.Deserialize(myFileStream);

                    if (!((IConfiguration)config).CheckDefaults(listaErrs))
                        throw new InvalidOperationException(string.Format(Constants.MissingConfigParam, listaErrs.FirstOrDefault()));
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(String.Format(Constants.XmlInvalid, e.Message));
                }
            }
            finally
            {
                if (myFileStream != null)
                    myFileStream.Close();
            }

            return config;
        }

        #endregion
    }
}
