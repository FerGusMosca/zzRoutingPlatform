using zHFT.Main.BusinessEntities.Orders;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class ExecutionReportDTO
    {
        #region Constructors

        public ExecutionReportDTO(ExecutionReport er)
        {
            ExecutionReport = er;
        }

        #endregion
        
        #region Public Attributes 
        
        public ExecutionReport ExecutionReport { get; set; }
        
        public string Msg = "ExecutionReportMsg";
        
        #endregion
    }
}