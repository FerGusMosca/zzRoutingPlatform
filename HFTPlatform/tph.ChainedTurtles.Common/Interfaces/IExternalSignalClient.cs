using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.Interfaces
{
    public interface IExternalSignalClient
    {
        #region Methods

        string EvalSignal(string featuresPayload);

        bool Connect();

        #endregion
    }
}
