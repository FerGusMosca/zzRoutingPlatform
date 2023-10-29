using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static zHFT.Main.Common.Util.Constants;

namespace tph.ConsoleStrategy.Common.Util
{
    public class CommandValidator
    {
        #region Public Static Methods

        public static void ValidateCommandParams(string cmd,string[] param, int minParams, int maxParams)
        {
            if (param.Length < minParams)
                throw new Exception($"Command {cmd} must have at least {minParams} params");


            if (param.Length > minParams && param.Length != maxParams)
                throw new Exception($"Command {cmd} on its extended version must have {maxParams} params");
        
        
        }

        public static string ExtractMandatoryParam(string[] param, int pos)
        {
            if (param.Length >= (pos + 1))
            {
                return param[pos];
            }
            else
                throw new Exception($"Invalid param position {pos}");


        }

        public static double   ExtractMandatoryDouble(string[] param, int pos)
        {
            try
            {
                if (param.Length >= (pos + 1))
                {
                    return Convert.ToDouble(param[pos]);
                }
                else
                    throw new Exception($"Invalid param position {pos}");
            }
            catch(Exception e)
            {
                throw new Exception($"Param {param[pos]} is not a valid double");


            }

        }

        public static string ExtractNonMandatoryParam(string[] param, int pos, string def=null)
        {
            if (param.Length >= (pos + 1))
            {
                return param[pos];
            }
            else
                return def;


        }

        #endregion
    }
}
