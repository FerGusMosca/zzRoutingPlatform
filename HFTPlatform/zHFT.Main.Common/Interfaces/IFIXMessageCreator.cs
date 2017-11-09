using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Interfaces
{
    public interface IFIXMessageCreator
    {
        QuickFix.Message RequestMarketData(int id, string symbol);

        QuickFix.Message RequestSecurityList(int secType, string security);

        void ProcessMarketData(QuickFix.Message snapshot, object security, OnLogMessage pOnLogMsg);
    }
}
