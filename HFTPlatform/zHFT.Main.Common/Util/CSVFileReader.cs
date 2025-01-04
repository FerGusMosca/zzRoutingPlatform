using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;

namespace zHFT.Main.Common.Util
{
    public class CSVFileReader
    {
        #region Public Static Methods

        public static EconomicSeriesValue[] ReadCSVDataSeries(string path,string dateFormat)
        {
            List< EconomicSeriesValue > economicSeriesValues = new List< EconomicSeriesValue >();
            string[] lines = File.ReadAllLines(path);


            // Iterar desde la segunda línea (índice 1)
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];

                // Dividir la línea en columnas usando la coma como delimitador
                string[] columns = line.Split(',');


                if (columns.Length == 2) // Verificar que hay dos columnas
                {
                    try
                    {
                        DateTime date = DateTime.ParseExact(columns[0], dateFormat, CultureInfo.InvariantCulture);
                        double val = Convert.ToDouble(columns[1]);

                        EconomicSeriesValue ecVal = new EconomicSeriesValue() { Date = date, Value = val };

                        economicSeriesValues.Add(ecVal);
                    }
                    catch (Exception ex) {

                        throw new Exception($"Could not convert to date/double the following line: {line}: {ex.Message}");
                    }
                }
                else
                {
                    throw new Exception($"Invalid value for line: {line}");                }
            }

            return economicSeriesValues.ToArray();

        }

        #endregion
    }
}
