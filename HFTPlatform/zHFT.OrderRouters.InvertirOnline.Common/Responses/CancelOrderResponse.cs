using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.OrderRouters.InvertirOnline.Common.Responses
{
    public class CancelOrderResponse
    {
        #region Public Attributes

        public bool? ok { get; set; }

        public ResponseMessage[] messages { get; set; }

        #endregion

        #region Public Methods

        public string GetError()
        {
            if (messages != null)
            {
                string resp = "";

                foreach (ResponseMessage message in messages)
                {

                    resp += message.description + ",";   
                
                }
                return resp;
            }
            else
                return "";
        }

        #endregion


    }
}
