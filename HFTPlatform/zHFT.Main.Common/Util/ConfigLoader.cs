using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.Common.Interfaces;

namespace zHFT.Main.Common.Util
{
    public class ConfigLoader
    {
        
        public static T GetConfiguration<T>(ILogger logger,string configFile, List<string> listaErrs)
        {

            logger.DoLog( string.Format("Creating Serializer {0}",configFile),Constants.MessageType.Information);
            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
            FileStream myFileStream = null;
            T config;
            try
            {
                try
                {
                    logger.DoLog( string.Format("Creating filestream @{0}",configFile),Constants.MessageType.Information);
                    myFileStream = new FileStream(configFile, FileMode.Open);
                    logger.DoLog( String.Format("Deserializing Config {0}",configFile),Constants.MessageType.Information);
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
        
        public static bool LoadConfig(ILogger logger, string configFile)
        {
            logger.DoLog(DateTime.Now.ToString() + "zHFT.Main.Common.Util.ConfigLoader.LoadConfig", Constants.MessageType.Information);

            logger.DoLog("Loading config:" + configFile, Constants.MessageType.Information);
            if (!File.Exists(configFile))
            {
                logger.DoLog(configFile + " does not exists", Constants.MessageType.Error);
                return false;
            }

            List<string> noValueFields = new List<string>();
            logger.DoLog("Processing config:" + configFile, Constants.MessageType.Information);
            try
            {
                logger.DoLoadConfig(configFile, noValueFields);
                logger.DoLog("Ending GetConfiguracion " + configFile, Constants.MessageType.Information);
            }
            catch (Exception e)
            {
                logger.DoLog("Error recovering config " + configFile + ": " + e.Message, Constants.MessageType.Error);
                return false;
            }

            if (noValueFields.Count > 0)
                noValueFields.ForEach(s => logger.DoLog(string.Format(Constants.FieldMissing, s), Constants.MessageType.Error));

            return true;
        }
    }
}
