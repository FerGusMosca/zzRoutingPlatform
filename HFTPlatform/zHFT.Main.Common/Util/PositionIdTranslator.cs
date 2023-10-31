using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Util
{
    public class PositionIdTranslator
    {
        #region Protected Attributes

        protected Dictionary<int, string> PosIdTranslator { get; set; }

        protected int NextPosId { get; set; }

        #endregion

        #region Constructors

        public PositionIdTranslator(int InitialPosId)
        {
            PosIdTranslator = new Dictionary<int, string>();
            NextPosId = InitialPosId;


        }

        #endregion

        #region Public Attrbutes

        public string GetNextGuidPosId()
        {
            lock (PosIdTranslator)
            {
                string guidPosId = Guid.NewGuid().ToString();
                PosIdTranslator.Add(NextPosId, guidPosId);
                NextPosId++;
                return guidPosId;
            }
        }

        public int GetFriendlyPosId(string guidPosId)
        {
            lock (PosIdTranslator)
            {
                if (PosIdTranslator.Values.Any(x => x == guidPosId))
                {
                    int friendlyPosId = PosIdTranslator.Keys.Where(x => PosIdTranslator[x] == guidPosId).FirstOrDefault();
                    return friendlyPosId;
                }
                else
                    throw new Exception($"There is not a Guid Pos Id for {guidPosId}");
            }

        }

        public string GetRealPosId(int friendlyPosId)
        {

            lock (PosIdTranslator)
            {
                if (PosIdTranslator.ContainsKey(friendlyPosId))
                {
                    return PosIdTranslator[friendlyPosId];
                }
                else
                    throw new Exception($"There is not a Friendly Pos Id for {friendlyPosId}");
            }

        }

        #endregion
    }
}
