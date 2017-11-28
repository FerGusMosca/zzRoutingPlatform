using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.BusinessEntities
{
    public class Account
    {
        #region Public Attributes

        public int Id { get; set; }

        public Customer Customer { get; set; }

        public long AccountNumber { get; set; }

        public Broker Broker { get; set; }

        public string Name { get; set; }

        public string GenericAccountNumber { get; set; }

        public decimal? Balance { get; set; }

        #region Interactive Brokers

        public string AccountDesc { get; set; }

        public string URL { get; set; }

        public long? Port { get; set; }

        public string Currency { get; set; }


        #endregion

        public string BrokerAccountName
        {
            get
            {
                if (Name != null && AccountDesc!= null)
                    return AccountDesc + " - " + Name;
                else if (Name != null)
                    return Name;
                else if (Name != null)
                    return Name;
                else
                    return "";

            }

        }

        #endregion
    }
}
