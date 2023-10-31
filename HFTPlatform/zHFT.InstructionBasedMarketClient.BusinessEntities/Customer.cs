using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.BusinessEntities
{
    public class Customer
    {
        #region Public Attributes

        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string DocumentType { get; set; }

        public long DocumentNumber { get; set; }

        public string Sex { get; set; }

        public DateTime? BirthDate { get; set; }

        public string Contact { get; set; }

        public string EMail { get; set; }


        #endregion
    }
}
