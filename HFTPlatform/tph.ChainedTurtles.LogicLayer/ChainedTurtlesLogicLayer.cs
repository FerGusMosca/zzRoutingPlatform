using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.Configuration;
using tph.DayTurtles.LogicLayer;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;

namespace tph.ChainedTurtles.LogicLayer
{
    public class ChainedTurtlesLogicLayer : tph.DayTurtles.LogicLayer.DayTurtles
    {

        #region Public Attributes

        public virtual ChainedConfiguration GetConfig()
        {
            return (ChainedConfiguration)Config;
        }

        #endregion

        #region Public Overriden Methods

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            this.ModuleConfigFile = configFile;
            this.OnMessageRcv += pOnMessageRcv;
            this.OnLogMsg += pOnLogMsg;
            StartTime = DateTimeManager.Now;
            LastCounterResetTime = StartTime;

            if (ConfigLoader.LoadConfig(this, configFile))
            {
                LoadCustomTurtlesWindows();

                base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);

                InitializeManagers(GetConfig().ConnectionString);

                Thread depuarateThread = new Thread(EvalDepuratingPositionsThread);
                depuarateThread.Start();

                return true;

            }
            else
            {
                return false;
            }
        }


        #endregion

    }
}
