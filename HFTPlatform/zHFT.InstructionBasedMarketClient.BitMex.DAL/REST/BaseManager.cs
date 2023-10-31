using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BitMex.BE;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO;


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

        protected zHFT.Main.Common.Enums.SecurityType GetSecurityTypeFromCode(string code)
        {
            if (code == _FFCCSX)
                return zHFT.Main.Common.Enums.SecurityType.FUT;
            else if (code == _FFWCSX)
                return zHFT.Main.Common.Enums.SecurityType.OTH;
            else if (code == _OCECCS)
                return zHFT.Main.Common.Enums.SecurityType.OPT;
            else if (code == _OPECCS)
                return zHFT.Main.Common.Enums.SecurityType.OPT;
            else if (code == _MRCXXX)
                return zHFT.Main.Common.Enums.SecurityType.IND;
            else if (code == _MRIXXX)
                return zHFT.Main.Common.Enums.SecurityType.IND;
            else if (code == _MRRXXX)
                return zHFT.Main.Common.Enums.SecurityType.IND;
            else
                return zHFT.Main.Common.Enums.SecurityType.OTH;
        }

        protected Security MapSecurity(Instrument instr)
        {
            Security sec = new Security();

            if (Security.GetSecurityTypeFromCode(instr.Typ) == SecurityType.FUT)
            {

                sec.Symbol = instr.Underlying;//Example: XBT
                sec.SecurityId = instr.Symbol;
                sec.QuoteSymbol = instr.QuoteCurrency;
                sec.SecurityType = SecurityType.FUT;
                sec.MaturityMonthYear = instr.Expiry.HasValue ? instr.Expiry.Value.ToString("yyyyMM") : null;
                sec.MaturityDate = instr.Expiry;
                sec.SecurityAltID = instr.Symbol;//Example: XBTZ8 <the future symbol> because this is how TT wants it
                sec.ContractMultiplier = Convert.ToDouble(instr.LotSize);
                sec.MinPriceIncrement = instr.TickSize;
                sec.MinPriceIncrementAmmount = instr.TickSize;
                sec.InitMargin = instr.InitMargin;
                sec.MaintMargin = instr.MaintMargin;
                sec.TakerFee = instr.TakerFee;
                sec.MakerFee = instr.MakerFee;
                sec.LotSize = instr.LotSize;
                sec.LastTrade = instr.LastPrice;

            }
            else if (Security.GetSecurityTypeFromCode(instr.Typ) == SecurityType.OPT)
            {
                sec.Symbol = instr.Underlying;//Example: XBT
                sec.SecurityId = instr.Symbol;
                sec.SecurityType = SecurityType.OPT;//Here we use the real security type. Later TT translation will have to be made
                //we don't say anything abou maturity date because there is not one.
                sec.SecurityAltID = instr.Symbol;//Example: XBTZ8 <the future symbol> because this is how TT wants it
                sec.StrikePrice = instr.OptionStrikePrice;
                sec.SecurityAltID = instr.Symbol;
                sec.QuoteSymbol = instr.QuoteCurrency;
                sec.ContractMultiplier = Convert.ToDouble(instr.LotSize);
                sec.MinPriceIncrement = instr.TickSize;
                sec.MinPriceIncrementAmmount = instr.TickSize;
                sec.MaturityMonthYear = instr.Expiry.HasValue ? instr.Expiry.Value.ToString("YYYYMM") : "";
                sec.MaturityDate = instr.Expiry;
                sec.InitMargin = instr.InitMargin;
                sec.MaintMargin = instr.MaintMargin;
                sec.TakerFee = instr.TakerFee;
                sec.MakerFee = instr.MakerFee;
                sec.LotSize = instr.LotSize;
                sec.LastTrade = instr.LastPrice;
            }
            else if (Security.GetSecurityTypeFromCode(instr.Typ) == SecurityType.INDX)
            {
                sec.Symbol = instr.Underlying;//Example: XBT
                sec.SecurityId = instr.Symbol;
                sec.QuoteSymbol = instr.QuoteCurrency;
                sec.SecurityType = SecurityType.INDX;//Here we use the real security type. Later TT translation will have to be made
                //we don't say anything abou maturity date because there is not one. 20991231 will be mapped on TT FIX side
                //contract multiplier will be set to 1 later in TT FIX translation
                sec.SecurityAltID = instr.Symbol;//Example: XBTZ8 <the future symbol> because this is how TT wants it
                sec.MinPriceIncrement = instr.TickSize;
                sec.MinPriceIncrementAmmount = instr.TickSize;
                sec.InitMargin = instr.InitMargin;
                sec.MaintMargin = instr.MaintMargin;
                sec.TakerFee = instr.TakerFee;
                sec.MakerFee = instr.MakerFee;
                sec.LotSize = instr.LotSize;
                sec.LastTrade = instr.LastPrice;
            }
            else if (Security.GetSecurityTypeFromCode(instr.Typ) == SecurityType.SWAP)
            {
                sec.Symbol = instr.Underlying;//Example: XBT
                sec.SecurityId = instr.Symbol;
                sec.QuoteSymbol = instr.QuoteCurrency;
                sec.SecurityType = SecurityType.SWAP;//Here we use the real security type. Later TT translation will have to be made
                //we don't say anything abou maturity date because there is not one. 20991231 will be mapped on TT FIX side
                //contract multiplier will be set to 1 later in TT FIX translation
                sec.SecurityAltID = instr.Symbol;
                sec.MinPriceIncrement = instr.TickSize;
                sec.MinPriceIncrementAmmount = instr.TickSize;
                sec.InitMargin = instr.InitMargin;
                sec.MaintMargin = instr.MaintMargin;
                sec.TakerFee = instr.TakerFee;
                sec.MakerFee = instr.MakerFee;
                sec.LotSize = instr.LotSize;
                sec.LastTrade = instr.LastPrice;
            }
            else//Cryptocurrency
            {
                sec.Symbol = instr.Symbol;
                sec.SecurityId = instr.Symbol;
                sec.QuoteSymbol = instr.QuoteCurrency;
                sec.SecurityType = Security.GetSecurityTypeFromCode(instr.Typ);
                sec.MinPriceIncrement = instr.TickSize;
                sec.MinPriceIncrementAmmount = instr.TickSize;
                sec.InitMargin = instr.InitMargin;
                sec.MaintMargin = instr.MaintMargin;
                sec.TakerFee = instr.TakerFee;
                sec.MakerFee = instr.MakerFee;
                sec.LotSize = instr.LotSize;
                sec.LastTrade = instr.LastPrice;
            }


            return sec;
        }

        protected MarketData MapMarketData(Instrument instr)
        {
            MarketData md = new MarketData();
            md.Security = new zHFT.Main.BusinessEntities.Securities.Security() 
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
