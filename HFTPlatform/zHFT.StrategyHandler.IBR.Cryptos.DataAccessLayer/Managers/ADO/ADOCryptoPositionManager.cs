using System;
using System.Data.SqlClient;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.DataAccessLayer.Managers.ADO;

namespace zHFT.StrategyHandler.IBR.Cryptos.DataAccessLayer.Managers.ADO
{
    public class ADOCryptoPositionManager: ADOPositionManager
    {
        #region Constructors
        
        public ADOCryptoPositionManager(string pConnectionString) : base(pConnectionString)
        {
        }
        
        #endregion
    }
}