using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Security_List;

namespace zHFT.StrategyHandler.SecurityListSaver.Common.Interfaces
{
    public interface ISecurityTranslator
    {
        //Traduzco todos los securities con códigos de mercados externos a los
        //códigos de SecuritiesHistoricalData
        void DoTranslate(SecurityList SecurityList);
       
    }
}
