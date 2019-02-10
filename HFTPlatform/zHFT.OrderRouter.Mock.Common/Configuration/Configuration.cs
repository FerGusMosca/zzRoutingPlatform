using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;

namespace zHFT.OrderRouter.Mock.Common.Configuration
{
    public class Configuration : BaseConfiguration
    {

        public string Name { get; set; }

        public int OrdeExecutionEveryNSeconds
        {
            get;
            set;
        }

        public override bool CheckDefaults(List<string> result)
        {
            return true;
        }
    }
}
