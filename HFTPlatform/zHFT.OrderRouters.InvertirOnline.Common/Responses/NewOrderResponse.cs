using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.OrderRouters.InvertirOnline.Common.Responses
{
    public class ResponseMessage 
    {

        public string title { get; set; }

        public string description { get; set; }
    
    }

    public class NewOrderResponse
    {
        #region Public Attributes

        public bool? ok { get; set; }

        public ResponseMessage[] messages { get; set; }

       

        public int? numeroOperacion { get; set; }

        public bool IsOk 
        { 
            get 
            {
                if (!ok.HasValue)
                {
                    return numeroOperacion.HasValue;
                }
                else
                    return ok.Value;
            } 
        
        }

        #endregion
    }
}
