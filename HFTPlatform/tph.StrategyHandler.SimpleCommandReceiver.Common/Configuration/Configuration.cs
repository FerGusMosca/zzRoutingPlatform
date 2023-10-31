using System.Collections.Generic;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Protected Attributes
        
        public string WebSocketURL { get; set; } 
        
        public string IncomingConfigPath { get; set; }
        
        public string IncomingModule { get; set; }
        
        public string OutgoingModule { get; set; }
        
        public string OutgoingConfigPath { get; set; }
        
        public bool SimulateCandlebars { get; set; }
        
        #endregion
        
        public override bool CheckDefaults(List<string> result)
        {
            if (string.IsNullOrEmpty(WebSocketURL))
            {
                result.Add("WebSocketURL");
                return false;
            }
            
            return true;
        }
    }
}