using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.DayTurtles.BusinessEntities
{
    public class TurtlesCustomConfig
    {
        #region Public Attributes


        public string Symbol { get; set; }

        public int OpenWindow { get; set; }

        public int CloseWindow { get; set; }

        public decimal? TakeProfitPct { get; set; }

        public bool ExitOnMMov { get; set; } 

        public bool ExitOnTurtles { get; set; }

        #endregion
    }
}
