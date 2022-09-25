using OrderBookLoaderMock.Common.DTO.Orders;

namespace OrderBookLoaderMock.Common.Interfaces
{
    public interface IOnExecutionReport
    {
        void OnExecutionReport(ExecutionReportMsg msg);
    }
}