using System;

namespace tph.OrderRouter.Cocos.Common.DTO.Accounts
{
    public class GetPositions
    {
        #region Protected Attributes
        
        public string comitente { get; set; }
        
        public string consolida { get; set; }
        
        public string proceso { get; set; }
        
        public DateTime? fechaDesde { get; set; }
        
        public DateTime? fechaHasta { get; set; }
        
        public string tipo { get; set; }
        
        public string especie { get; set; }
        
        public string comitenteMana { get; set; }
        
        #endregion
    }
}