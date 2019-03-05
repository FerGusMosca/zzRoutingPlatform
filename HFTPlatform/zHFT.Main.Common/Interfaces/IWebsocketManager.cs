using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Interfaces
{
    public interface IWebsocketManager
    {
        void OnEvent (string msg);
        
    }
}
