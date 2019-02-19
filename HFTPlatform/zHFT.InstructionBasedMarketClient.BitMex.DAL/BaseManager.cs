using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;


namespace zHFT.InstructionBasedMarketClient.BitMex.DAL
{
    public class BaseManager
    {
        #region Private Consts

        private static string _FFCCSX = "FFCCSX";

        private static string _FFWCSX = "FFWCSX";

        private static string _OCECCS = "OCECCS";

        private static string _OPECCS = "OPECCS";

        //Type for indexs

        private static string _MRCXXX = "MRCXXX";

        private static string _MRIXXX = "MRIXXX";

        private static string _MRRXXX = "MRRXXX";

        #endregion

        #region Protected Consts

        protected static string _URL_POSTFIX = "/api/v1";

        #endregion

        #region Protected Methods

        protected  SecurityType GetSecurityTypeFromCode(string code)
        {
            if (code == _FFCCSX)
                return SecurityType.FUT;
            else if (code == _FFWCSX)
                return SecurityType.OTH;
            else if (code == _OCECCS)
                return SecurityType.OPT;
            else if (code == _OPECCS)
                return SecurityType.OPT;
            else if (code == _MRCXXX)
                return SecurityType.IND;
            else if (code == _MRIXXX)
                return SecurityType.IND;
            else if (code == _MRRXXX)
                return SecurityType.IND;
            else
                return SecurityType.OTH;
        }


        protected MarketData MapMarketData(Instrument instr)
        {
            MarketData md = new MarketData();
            md.Security = new Security() 
                                        { 
                                            Symbol = instr.Symbol, 
                                            UnderlyingSymbol = instr.Underlying,
                                            Currency=instr.QuoteCurrency,
                                            SecType=GetSecurityTypeFromCode(instr.Typ)
                                        };

            md.OpeningPrice = instr.OpenValue;
            md.ClosingPrice = instr.PrevClosePrice;

            md.TradingSessionHighPrice = instr.HighPrice;
            md.TradingSessionLowPrice = instr.LowPrice;

            md.OpenInterest = instr.OpenInterest;

            md.TradeVolume = instr.Volume;

            md.Trade = instr.LastPrice;

            md.SettlementPrice = instr.SettledPrice;

            md.BestBidPrice = instr.BidPrice;
            md.BestAskPrice = instr.AskPrice;

            md.CashVolume = instr.TotalVolume;

            return md;
        }

        #endregion
    }
}
