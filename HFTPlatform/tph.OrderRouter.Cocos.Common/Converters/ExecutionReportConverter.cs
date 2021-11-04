using System;
using System.Collections.Generic;
using tph.OrderRouter.Cocos.Common.DTO.Orders;
using zHFT.Main.BusinessEntities.Orders;

namespace tph.OrderRouter.Cocos.Common.Converters
{
    public class ExecutionReportConverter
    {
        #region Public Static Attributes

        public static zHFT.Main.BusinessEntities.Orders.ExecutionReport[] ConvertExecutionReports(ExecutionReportsResponse resp)
        {
            List<zHFT.Main.BusinessEntities.Orders.ExecutionReport> tphExecutionReports = new List<zHFT.Main.BusinessEntities.Orders.ExecutionReport>();
            foreach (tph.OrderRouter.Cocos.Common.DTO.Orders.ExecutionReport ccExecReportList in resp.Result)
            {
                foreach (ListaDetalleTicker security in ccExecReportList.listaDetalleTiker)
                {
                    foreach (ExecutionReportOrder ccExecReport in security.ORDE)
                    {
                        try
                        {
                            zHFT.Main.BusinessEntities.Orders.ExecutionReport tphExecReport = new  zHFT.Main.BusinessEntities.Orders.ExecutionReport();
                        
                            tphExecutionReports.Add(tphExecReport);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                        
                    }
                }
            }

            return tphExecutionReports.ToArray();
        }

        #endregion
    }
}