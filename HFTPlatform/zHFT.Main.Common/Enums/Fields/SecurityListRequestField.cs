using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public class SecurityListRequestField: Fields
    {
        public static readonly SecurityListRequestField Symbol = new SecurityListRequestField(2);
        public static readonly SecurityListRequestField SecurityListRequestType = new SecurityListRequestField(3);



        protected SecurityListRequestField(int pInternalValue)
            : base(pInternalValue)
        {

        }
    }
}
