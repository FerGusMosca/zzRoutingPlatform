using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.InstructionBasedMarketClient.BusinessEntities
{
    public enum Side
    {
        Buy = '1',
        Sell = '2'

    }

    public class Instruction
    {
        #region Public Methods

        public int Id { get; set; }

        public DateTime Date { get; set; }

        public InstructionType InstructionType { get; set; }

        //Agregar el atributo del model portfolio

        public AccountPosition AccountPosition { get; set; }

        public Account Account { get; set; }

        public bool Executed { get; set; }

        public Side? Side { get; set; }

        public int? Shares { get; set; }

        public string Symbol { get; set; }

        public SecurityType SecurityType { get; set; }

        public decimal? Ammount { get; set; }

        public Instruction RelatedInstruction { get; set; }


        #endregion
    }
}
