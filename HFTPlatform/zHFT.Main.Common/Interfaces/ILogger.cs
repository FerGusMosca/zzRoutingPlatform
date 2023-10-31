using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Util;

namespace zHFT.Main.Common.Interfaces
{
    public interface ILogger
    {
        void DoLog(string msg, Constants.MessageType type);

        void DoLoadConfig(string configFile, List<string> listaCamposSinValor);
    }
}
