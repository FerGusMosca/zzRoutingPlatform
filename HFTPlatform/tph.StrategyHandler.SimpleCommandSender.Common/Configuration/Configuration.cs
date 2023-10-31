using System.Collections.Generic;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace tph.StrategyHandler.SimpleCommandSender.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Protected Attributes
        
        public string WebSocketURL { get; set; } 
        
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