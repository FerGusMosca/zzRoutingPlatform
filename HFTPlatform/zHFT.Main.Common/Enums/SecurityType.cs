using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public enum SecurityType
    {
        CS,//Common Stock
        FUT,//Future
        OPT,//Options
        IND,//INDEX
        CASH,//Cash,
        TBOND,//Treasury Bond - non US
        TB,//Treasury Bill - non US
        IRS,// Interest Rate Swap
        REPO,//Repurchase,
        CC,//CryptoCurrency
        OTH,//Other,
        SWAP,
        MF
    }
}
