using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouter.Mock.Common;

namespace zHFT.OrderRouters.Mock
{
    public class OrderRouter : OrderRouterBase
    {
        #region Public Methods
        public override CMState ProcessMessage(Wrapper wrapper)
        {
            DoLog("Mock processing of order routing... " , Main.Common.Util.Constants.MessageType.Information);
            return CMState.BuildSuccess();
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {

            this.ModuleConfigFile = configFile;
            this.OnMessageRcv += pOnMessageRcv;
            this.OnLogMsg += pOnLogMsg;

            DoLog("Mock Order Router Initializing... " , Main.Common.Util.Constants.MessageType.Information);
            return true;
        }
        #endregion
    }
}
