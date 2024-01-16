using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.DTO;

namespace zHFT.StrategyHandler.Common.Converters
{
    public class EconomicSeriesConverter
    {
        #region Public Static Methods

        public static EconomicSeriesDTO ConvertEconomicSeries(Wrapper wrapper)
        {
            EconomicSeriesDTO respDTO = new EconomicSeriesDTO();
            if (wrapper.GetField(EconomicSeriesField.SeriesID) != EconomicSeriesField.NULL)
                respDTO.SeriesID = (string)wrapper.GetField(EconomicSeriesField.SeriesID);
            else
                throw new Exception($"Missing mandatory field for economic series!:SeriesID");


            if (wrapper.GetField(EconomicSeriesField.From) != EconomicSeriesField.NULL)
                respDTO.From = (DateTime)wrapper.GetField(EconomicSeriesField.From);
            else
                throw new Exception($"Missing mandatory field for economic series!:From");

            if (wrapper.GetField(EconomicSeriesField.To) != EconomicSeriesField.NULL)
                respDTO.To = (DateTime)wrapper.GetField(EconomicSeriesField.To);
            else
                throw new Exception($"Missing mandatory field for economic series!:To");

            if (wrapper.GetField(EconomicSeriesField.To) != EconomicSeriesField.NULL)
                respDTO.To = (DateTime)wrapper.GetField(EconomicSeriesField.To);
            else
                throw new Exception($"Missing mandatory field for economic series!:To");


            if (wrapper.GetField(EconomicSeriesField.To) != EconomicSeriesField.NULL)
                respDTO.Values = (List<EconomicSeriesValue>)wrapper.GetField(EconomicSeriesField.Values);
            else
                throw new Exception($"Missing mandatory field for economic series!:Values");


            if (wrapper.GetField(EconomicSeriesField.Interval) != EconomicSeriesField.NULL)
                respDTO.Interval = (CandleInterval)wrapper.GetField(EconomicSeriesField.Interval);
            else
                throw new Exception($"Missing mandatory field for economic series!:Interval");


            if (wrapper.GetField(EconomicSeriesField.Success) != EconomicSeriesField.NULL)
                respDTO.Success = (bool)wrapper.GetField(EconomicSeriesField.Success);
            else
                throw new Exception($"Missing mandatory field for economic series!:Success");

            if (wrapper.GetField(EconomicSeriesField.Error) != EconomicSeriesField.NULL)
                respDTO.Error = (string)wrapper.GetField(EconomicSeriesField.Error);
            else
                respDTO.Error = null;


            return respDTO;

        }


        #endregion
    }
}
