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
        public static readonly SecurityListRequestField Exchange = new SecurityListRequestField(4);
        public static readonly SecurityListRequestField Currency = new SecurityListRequestField(5);
        public static readonly SecurityListRequestField SecurityType = new SecurityListRequestField(6);



        protected SecurityListRequestField(int pInternalValue)
            : base(pInternalValue)
        {

        }
    }
}
