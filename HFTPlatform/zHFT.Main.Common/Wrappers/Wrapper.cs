using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.Common.Wrappers
{
    public abstract class Wrapper
    {
        #region Public Abstract Methods

        public abstract object GetField(Fields field);

        public abstract Actions GetAction();

        #endregion

        #region Public Methods

        public virtual string ToString()
        {
            return "Wrapper";
        }
        #endregion
    }
}
