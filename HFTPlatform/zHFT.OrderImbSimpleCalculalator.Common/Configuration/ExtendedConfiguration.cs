using System.Collections.Generic;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Enums;

namespace zHFT.OrderImbSimpleCalculator.Common.Configuration
{
    public class ExtendedConfiguration:Configuration
    {
        #region Public Attributes
        
        public string CandleReferencePrice { get; set; }
        
        public string MaxOpeningTime { get; set; }
        
        public int MaxMinWaitBtwConsecutivePos { get; set; }
        
        #endregion
        
        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (StocksToMonitor.Count==0)
            {
                result.Add("StocksToMonitor");
                resultado = false;
            }

            if (string.IsNullOrEmpty(ConnectionString))
            {
                result.Add("ConnectionString");
                resultado = false;
            }

            if (string.IsNullOrEmpty(SaveEvery))
            {
                result.Add("SaveEvery");
                resultado = false;
            }

            if (string.IsNullOrEmpty(OrderRouter))
            {
                result.Add("OrderRouter");
                resultado = false;
            }

            if (string.IsNullOrEmpty(OrderRouter))
            {
                result.Add("OrderRouter");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Currency))
            {
                result.Add("Currency");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Exchange))
            {
                result.Add("Exchange");
                resultado = false;
            }

            if (string.IsNullOrEmpty(FeeTypePerTrade))
            {
                result.Add("FeeTypePerTrade");
                resultado = false;
            }

            if (string.IsNullOrEmpty(SecurityTypes))
            {
                result.Add("SecurityTypes");
                resultado = false;
            }
            
            
            if (string.IsNullOrEmpty(ClosingTime))
            {
                result.Add("ClosingTime");
                resultado = false;
            }



            if (SecurityTypes == SecurityType.FUT.ToString())
            {

            }
            else
            {

                if (!PositionSizeInCash.HasValue)
                {
                    result.Add("PositionSizeInCash");
                    resultado = false;
                }
            }

            return resultado;
        }


        #endregion
        
    }
}