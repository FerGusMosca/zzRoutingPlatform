using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.OptionsMarketClient.Common.Configuration
{
    public enum StrikeFilter
    {
        ITM = 'I',
        OTM = 'O',
        All = 'A'
    }


    public enum PutOrCall
    {
        PUT = 'P',
        CALL = 'C',
        All = 'A'
    }


    public class Configuration : BaseConfiguration, IConfiguration
    {

        #region Public Attributes

        public bool Active { get; set; }

        public string IP { get; set; }

        public int Port { get; set; }

        public string OptionsConverter { get; set; }

        public string AccessLayerConnectionString { get; set; }

        public int PublishUpdateInMilliseconds { get; set; }

        public int SecurityUpdateInHours { get; set; }

        public int IdIBClient { get; set; }

        public string Currency { get; set; }

        public string SecType { get; set; }

        public string Exchange { get; set; }

        public string MarketStartTime { get; set; }

        public string MarketEndTime { get; set; }

        public string ContractFilter { get; set; }

        public int MinMaturityDistanceInMonths { get; set; }

        public int MaxMaturityDistanceInMonths { get; set; }

        public string StrStrikeFilter { get; set; }

        public StrikeFilter? StrikeFilter
        {
            get 
            {

                if (StrStrikeFilter != null)
                    return (StrikeFilter)Convert.ToChar(StrStrikeFilter);
                else
                    return null;
            }
        
        }


        public string StrPutOrCall { get; set; }

        public PutOrCall? PutOrCall
        {
            get
            {

                if (StrPutOrCall != null)
                    return (PutOrCall)Convert.ToChar(StrPutOrCall);
                else
                    return null;
            }

        }

        public int MaxContractsPerSecurity { get; set; }

        public int MaxContractsInSession { get; set; }

        public bool ForceContractRecovery { get; set; }

        #endregion

        #region Private Methods

        public bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (string.IsNullOrEmpty(Name))
            {
                result.Add("Name");
                resultado = false;
            }

            if (string.IsNullOrEmpty(IP))
            {
                result.Add("IP");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Currency))
            {
                result.Add("Currency");
                resultado = false;
            }

            if (string.IsNullOrEmpty(OptionsConverter))
            {
                result.Add("OptionsConverter");
                resultado = false;
            }

            if (string.IsNullOrEmpty(SecType))
            {
                result.Add("SecType");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Exchange))
            {
                result.Add(Exchange);
                resultado = false;
            }

            if (Port < 0)
            {
                result.Add("Port");
                resultado = false;
            }

            if (PublishUpdateInMilliseconds <= 0)
            {
                result.Add("PublishUpdateInMilliseconds");
                resultado = false;
            }

            if (SecurityUpdateInHours <= 0)
            {
                result.Add("SecurityUpdateInHours");
                resultado = false;
            }

            if (IdIBClient <= 0)
            {
                result.Add("IdIBClient");
                resultado = false;
            }

            if (string.IsNullOrEmpty(MarketStartTime))
            {
                result.Add("MarketStartTime");
                resultado = false;
            }

            if (string.IsNullOrEmpty(MarketEndTime))
            {
                result.Add("MarketEndTime");
                resultado = false;
            }

            if (string.IsNullOrEmpty(ContractFilter))
            {
                result.Add("ContractFilter");
                resultado = false;
            }

            if (MinMaturityDistanceInMonths<=0)
            {
                result.Add("MinMaturityDistanceInMonths");
                resultado = false;
            }

            if (MaxMaturityDistanceInMonths <= 0)
            {
                result.Add("MaxMaturityDistanceInMonths");
                resultado = false;
            }

            if (MaxContractsPerSecurity <= 0)
            {
                result.Add("MaxContractsPerSecurity");
                resultado = false;
            }

            if (MaxContractsInSession <= 0)
            {
                result.Add("MaxContractsInSession");
                resultado = false;
            }

            if (StrikeFilter==null)
            {
                result.Add("StrikeFilter");
                resultado = false;
            }

            if (PutOrCall == null)
            {
                result.Add("PutOrCall");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}
