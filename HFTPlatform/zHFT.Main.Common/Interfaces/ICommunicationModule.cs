using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;

namespace zHFT.Main.Common.Interfaces
{
    public delegate CMState OnMessageReceived(Wrapper wrapper);

    public delegate void OnLogMessage(string msg, Constants.MessageType type);

    public interface ICommunicationModule
    {
        CMState ProcessMessage(Wrapper wrapper);

        bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile);
    }
}
