using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{
    public class ExternalSignalTurtleIndicatorConfigDTO: TurtleIndicatorBaseConfigDTO
    {
        #region Public Attributes

        public string extSignalAssembly { get; set; }

        public Dictionary<string,string> commConfig { get; set; }


        #endregion
    }
}
