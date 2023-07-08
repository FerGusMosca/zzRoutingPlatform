using zHFT.StrategyHandler.Common;

namespace tph.DayTurtles.Common.Configuration
{
    public class DayTurtlesConfiguration:BaseStrategyConfiguration
    {
        #region Public Attributes
        
        public int OpenWindow { get; set; }
        
        public int CloseWindow { get; set; }
        
        public string ConnectionString { get; set; }
        
        
        #endregion
    }
}