using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.SingletonModulesHandler.Common.Interfaces;

namespace zHFT.OrderRouters.Router
{
    public class SingletonOrderRouter : OrderRouter, ISingletonModule
    {
        void ISingletonModule.DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            base.DoLoadConfig(configFile,listaCamposSinValor);
        }

        void ISingletonModule.DoLog(string msg, Constants.MessageType type)
        {
            base.DoLog(msg,type);
        }

        void ISingletonModule.SetIncomingEvent(OnMessageReceived OnMessageRcv)
        {
            if(this.OnMessageRcv==null)
                this.OnMessageRcv += OnMessageRcv;
        }

        void ISingletonModule.SetOutgoingEvent(OnMessageReceived OnMessageRcv)
        {
            if (this.OnMessageRcv == null)
                this.OnMessageRcv += OnMessageRcv;
        }
    }
}
