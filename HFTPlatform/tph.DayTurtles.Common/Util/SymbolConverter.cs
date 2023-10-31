using System;

namespace tph.DayTurtles.Common.Util
{
    public class SymbolConverter
    {
        #region Public Attributes

        protected static string _SYMBOL_SEP = ".";
        
        #endregion
        
        #region Public Methods

        public static string GetCleanSymbol(string symbol)
        {
            if (symbol.Contains(_SYMBOL_SEP))
            {
                return symbol.Split(new string[] {_SYMBOL_SEP}, StringSplitOptions.RemoveEmptyEntries)[0];
            }
            else
            {
                return symbol;
            }
            
        }

        #endregion
    }
}