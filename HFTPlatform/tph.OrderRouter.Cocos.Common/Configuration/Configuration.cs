using System.Collections.Generic;
using zHFT.Main.Common.Abstract;

namespace tph.OrderRouter.Cocos.Common
{
    public class Configuration : BaseConfiguration
    {
        #region Public Attributes
        
        public string BaseURL { get; set; }
        
        public string DNI { get; set; }
        
        public string User { get; set; }

        public string Password { get; set; }

        #endregion

        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool res = true;

            
            if (string.IsNullOrEmpty(BaseURL))
            {
                result.Add("BaseURL");
                res = false;
            }
            
            if (string.IsNullOrEmpty(DNI))
            {
                result.Add("DNI");
                res = false;
            }
            
            if (string.IsNullOrEmpty(User))
            {
                result.Add("User");
                res = false;
            }
            
            if (string.IsNullOrEmpty(Password))
            {
                result.Add("Password");
                res = false;
            }

           
            return res;
        }

        #endregion
    }
}