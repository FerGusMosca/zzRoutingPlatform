using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;

namespace zHFT.SingletonModulesHandler.Common.Interfaces
{
    public interface ISingletonModule
    {
        void DoLoadConfig(string configFile, List<string> listaCamposSinValor);

        void DoLog(string msg, Constants.MessageType type);

        void SetOutgoingEvent(OnMessageReceived OnMessageRcv);

        void SetIncomingEvent(OnMessageReceived OnMessageRcv);

        CMState ProcessMessage(Wrapper wrapper);
    }
}
