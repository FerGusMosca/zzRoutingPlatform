using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.DTO;

namespace tph.ChainedTurtles.Common.Interfaces
{
    public interface IExternalSignalClient
    {
        #region Methods

        TimestampRangeClassificationDTO EvalSignal(string ctxPayload=null);

        bool Connect();

        #endregion
    }
}
