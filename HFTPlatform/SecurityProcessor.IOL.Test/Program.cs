using SecurityProcessor.IOL.Test.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstrFullMarketConnectivity.IOL.BusinessEntities;
using zHFT.InstructionBasedMarketClient.IOL.Common.DTO;
using zHFT.InstructionBasedMarketClient.IOL.DataAccessLayer;
using zHFT.Main.Common.Util;

namespace SecurityProcessor.IOL.Test
{
    class Program
    {

        protected static void OnLogMessage(string msg, Constants.MessageType type)
        {

            Console.WriteLine(msg);
        }


        static void Main(string[] args)
        {
            string mainURL = ConfigurationManager.AppSettings["MainURL"];
            string connectionString = ConfigurationManager.AppSettings["ConnectionString"];

            SecurityListManager secListMgr = new SecurityListManager(connectionString);

            AccountInvertirOnlineData cred = new AccountInvertirOnlineData();
            cred.User = ConfigurationManager.AppSettings["Login"];
            cred.Password = ConfigurationManager.AppSettings["Pwd"];


            OnLogMessage("Requesting Security List...", Constants.MessageType.Information);
            IOLSecurityManager secMgr = new IOLSecurityManager(OnLogMessage, mainURL, cred);

            secMgr.GetSecurities("argentina");

            OnLogMessage("Requesting Funds...", Constants.MessageType.Information);
            Fund[] fundArr = secMgr.GetFunds();
            OnLogMessage(string.Format("Persisting {0} funds...",fundArr.Length), Constants.MessageType.Information);
            fundArr.ToList().ForEach(x => secListMgr.PersistFund(x));

            Console.ReadKey();
            
        }
    }
}
