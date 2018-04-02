using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.StrategyHandler.SecurityListSaver.Common.Interfaces;

namespace zHFT.StrategyHandler.SecurityListSaver.Primary.LogicLayer.Translators
{
    public class PrimarySecurityTranslator : ISecurityTranslator
    {
        #region Private static Consts

        private string _PRIMARY_BYMA_CODE = "MERV";
        private string _PRIMARY_ROFX_CODE = "ROFX";
        private string _SECURITIES_HISTORICAL_DATA_BYMA_CODE = "BUE";

        #endregion

        #region Public Methods

        public void DoTranslate(zHFT.Main.BusinessEntities.Security_List.SecurityList SecurityList)
        {
            //Todos los códigos propios del mundo Primary
            //Lo traducimos a los códigos SecuritiesHistoricalData

            foreach (Security sec in SecurityList.Securities)
            {
                sec.Symbol = sec.Symbol.Trim();

                if (sec.Exchange.Trim() == _PRIMARY_BYMA_CODE)
                    sec.Exchange = _SECURITIES_HISTORICAL_DATA_BYMA_CODE;

                if (sec.SecType == SecurityType.FUT)
                {
                    if (sec.Exchange.Trim() == _PRIMARY_ROFX_CODE)
                        sec.Symbol = sec.Symbol + "." + _PRIMARY_ROFX_CODE;
                }

                //Los demas códigos no existen en el mundo SecuritiesHistoricalData así que 
                // por ahora no lo traducimos
            }

        }

        #endregion
    }
}
