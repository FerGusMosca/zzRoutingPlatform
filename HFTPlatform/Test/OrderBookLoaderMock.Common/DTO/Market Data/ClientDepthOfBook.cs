using System;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;

namespace OrderBookLoaderMock.Common.DTO
{
    public class ClientDepthOfBook:WebSocketMessage
    {
        #region Public Static Consts

        public static string _ACTION_INSERT = "I";

        public static string _ACTION_UPDATE = "U";

        public static string _ACTION_DELETE = "D";
        
        #endregion
        
        #region Public Attributes

        public string Symbol { get; set; }
        
        public string Side { get; set; }
        
        public int Index { get; set; }
        
        public double? Price { get; set; }
        
        public double? Size { get; set; }
        
        public string Action { get; set; }
        
        public long Timestamp { get; set; }
        
        public string UUID { get; set; }

        #endregion
        
        public override string ToString()
        {
            string resp="";

            resp += String.Format(" Symbol={0} ",Symbol);
            resp += String.Format(" Side={0} ",Side);
            resp += String.Format(" Index={0} ",Index);
            resp += String.Format(" Price={0} ",Price);
            resp += String.Format(" Size={0} ",Size);
            resp += String.Format(" Action={0} ",Action);
            resp += String.Format(" Timestamp={0} ",Timestamp);
            resp += String.Format(" UUID={0} ",UUID);

            return resp;
        }
    }
}