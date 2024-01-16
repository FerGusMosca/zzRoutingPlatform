using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.DTO;

namespace zHFT.StrategyHandler.Common.Converters
{
    public class EconomicSeriesRequestConverter
    {
        #region Public Static Methods

        public static EconomicSeriesRequestDTO ConvertEconomicSeriesRequest(Wrapper wrapper)
        {
            EconomicSeriesRequestDTO respDTO = new EconomicSeriesRequestDTO();
            if (wrapper.GetField(EconomicSeriesRequestField.SeriesID) != EconomicSeriesRequestField.NULL)
                respDTO.SeriesID = (string)wrapper.GetField(EconomicSeriesRequestField.SeriesID);
            else
                throw new Exception($"Missing mandatory field requesting econmic serieres!:SeriesID");


            if (wrapper.GetField(EconomicSeriesRequestField.From) != EconomicSeriesRequestField.NULL)
                respDTO.From = (DateTime)wrapper.GetField(EconomicSeriesRequestField.From);
            else
                throw new Exception($"Missing mandatory field requesting econmic serieres!:From");

            if (wrapper.GetField(EconomicSeriesRequestField.To) != EconomicSeriesRequestField.NULL)
                respDTO.To = (DateTime)wrapper.GetField(EconomicSeriesRequestField.To);
            else
                throw new Exception($"Missing mandatory field requesting econmic serieres!:To");



            return respDTO;

        }

        #endregion
    }
}
