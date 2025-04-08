using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.DTO;


namespace zHFT.InstructionBasedFullMarketConnectivity.Common
{
    public interface IPortfolioPositionConverter
    {
       IEnumerable<Position> ConvertPositions(GenericResponse resp,string accountNumber, 
                                              bool useCleanSymbol,
                                              Dictionary<string, zHFT.Main.Common.Enums.SecurityType> secTypes);

       IEnumerable<zHFT.Main.BusinessEntities.Positions.Position> ConvertAccountReport(GenericResponse resp, string accountNumber);
    }
}
