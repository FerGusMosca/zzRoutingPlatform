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

        public string IBAccount { get; set; }

        public string IBURL { get; set; }

        public long? IBPort { get; set; }

        public decimal? IBBalance { get; set; }

        public string IBCurrency { get; set; }

        public string IBName { get; set; }

        public string BrokerAccountName
        {
            get
            {
                if (IBName != null && IBAccount != null)
                    return IBAccount + " - " + IBName;
                else if (IBName != null)
                    return IBName;
                else
                    return "";

            }

        }

        #endregion
    }
}
