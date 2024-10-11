using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{
    public class MockClassificationDTO : TimestampRangeClassificationDTO
    {
        public override bool IsLongSignalTriggered()
        {
            return true;
        }

        public override bool IsShortSignalTriggered()
        {
            return true;
        }
    }
}
