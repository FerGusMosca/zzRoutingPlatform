using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.DTO;
using tph.ChainedTurtles.Common.Interfaces;
using zHFT.Main.Common.Interfaces;
using static zHFT.Main.Common.Util.Constants;

namespace tph.ChainedTurtles.DataAccessLayer
{
    public class OnOffValveManager : IExternalSignalClient
    {

        #region     Protected Consts

        protected static string _GET_TIMESTAMP_RANGE_CLASSIFICATIONS = "GetTimestampRangeClassifications";

        #endregion

        #region Protected Attributes

        protected DataBaseConnectionConfigDTO Config { get; set; }

        protected ILogger Logger { get; set; }


        protected OnOffTimestampRangeClassificationDTO CurrentSignal { get; set; }

        protected DateTime? LastRenewedSignal { get; set; }

        #endregion

        #region Constructors

        public OnOffValveManager(Dictionary<string, string> extSignalConfig,ILogger pLogger) {


            try
            {
              
                Config= LoadConfigValues(extSignalConfig);

                if (pLogger != null)
                    Logger = pLogger;
                else
                    throw new Exception($"OnOffValveManager did not received a properly initialized logger!");

               
                LastRenewedSignal = null;
                Logger.DoLog($"OnOffValveManager successfully initialized", MessageType.Information);

            }
            catch (Exception ex) {

                string msg = $"CRITICAL ERORR initializing OnOfValveManager : {ex.Message}";

                Logger.DoLog(msg, MessageType.Error);

                throw new Exception(msg);
            
            }
        }

        #endregion

        #region Protected Methods


        protected DataBaseConnectionConfigDTO LoadConfigValues(Dictionary<string, string> extSignalConfig)
        {

            //We instantiate the config class
            DataBaseConnectionConfigDTO config = new DataBaseConnectionConfigDTO();

            if (extSignalConfig.ContainsKey("connectionString"))
                config.connectionString = extSignalConfig["connectionString"];
            else
                throw new Exception($"Could not find config value connectionString at OnOffValveManager settings");

            if (extSignalConfig.ContainsKey("refreshEveryNMinutes"))
            {
                try
                {
                    config.refreshEveryNMinutes = Convert.ToInt32(extSignalConfig["refreshEveryNMinutes"]);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Invalid value for attribute refreshEveryNMinutes");
                }
            }
            else
                throw new Exception($"Could not find config value refreshEveryNMinutes at OnOffValveManager settings");

            return config;
        }

        protected OnOffTimestampRangeClassificationDTO BuildOnOffTimestampRangeClassificationDTO(SqlDataReader reader)
        {

            OnOffTimestampRangeClassificationDTO dto = new OnOffTimestampRangeClassificationDTO()
            {
                id=Convert.ToInt64(reader["id"]),
                key = reader["key"].ToString(),
                TimestampStart = Convert.ToDateTime(reader["timestamp_start"]),
                TimestampEnd     = Convert.ToDateTime(reader["timestamp_end"]),
                Classification = reader["classification"].ToString()
            };


            dto.Validate();

            return dto;

        }


        protected bool MustFetchSignal()
        {
            if (LastRenewedSignal.HasValue)
            {
                TimeSpan elapsed = DateTime.Now - LastRenewedSignal.Value;
                return elapsed.TotalMinutes > Config.refreshEveryNMinutes;
            }
            else
                return true;
        
        
        }

        protected List<OnOffTimestampRangeClassificationDTO> DoFetchRangeClassifications(DateTime start, DateTime end)
        {
            //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_GET_TIMESTAMP_RANGE_CLASSIFICATIONS, new SqlConnection(Config.connectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Key", OnOffTimestampRangeClassificationDTO._ON_OFF_CLASSIF);
            cmd.Parameters["@Key"].Direction = ParameterDirection.Input;


            cmd.Parameters.AddWithValue("@From", start);
            cmd.Parameters["@From"].Direction = ParameterDirection.Input;


            cmd.Parameters.AddWithValue("@To", end);
            cmd.Parameters["@To"].Direction = ParameterDirection.Input;

            cmd.Connection.Open();

            // Open DB
            SqlDataReader reader;
            List<OnOffTimestampRangeClassificationDTO> onOffClassifications = new List<OnOffTimestampRangeClassificationDTO>();

            try
            {
                // Run Query
                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        onOffClassifications.Add(BuildOnOffTimestampRangeClassificationDTO(reader));
                    }
                }
            }
            finally
            {
                cmd.Connection.Close();
            }

            return onOffClassifications;

        }

        #endregion

        #region Public Methods

        public bool Connect()
        {
            return true;//DB connections are done when evaluating the signal
        }


        public TimestampRangeClassificationDTO EvalSignal(string ctxPayload=null)
        {
            try
            {
                Logger.DoLog($"Eval Signal at OnOffValveManager", MessageType.Information);

                if (MustFetchSignal())
                {
                    Logger.DoLog($"Fetching at database for new on/off signal at OnOffValveManager", MessageType.Information);
                    DateTime start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                    DateTime end = start.AddMinutes(Config.refreshEveryNMinutes);//Range of N minutes

                    List<OnOffTimestampRangeClassificationDTO> classifs = DoFetchRangeClassifications(start, end);
                    

                    OnOffTimestampRangeClassificationDTO currClasif = classifs.Where(x => DateTime.Compare(x.TimestampStart, DateTime.Now) <= 0
                                                                                         && DateTime.Compare(DateTime.Now, x.TimestampEnd) <= 0).FirstOrDefault();

                    if (currClasif != null)
                    {
                        Logger.DoLog($"On/Off signal renewed at OnOffValveManager: Signal={currClasif.Classification}", MessageType.Information);

                        CurrentSignal = currClasif;
                    }
                    else
                    {
                        CurrentSignal = OnOffTimestampRangeClassificationDTO.BuildOffSignal();
                        Logger.DoLog($"On/Off signal not found for OnOffValveManager: Creating Signal={CurrentSignal.Classification}", MessageType.Information);
                    }

                    LastRenewedSignal = DateTime.Now;
                }
                
                return CurrentSignal;
            }
            catch (Exception ex)
            {
                Logger.DoLog($"ERROR for On/Off at OnOffValveManager: {ex.Message}. Creating Off Signal", MessageType.Error);
                CurrentSignal = OnOffTimestampRangeClassificationDTO.BuildOffSignal();
                return CurrentSignal;
            }
        }


        #endregion
    }
}
