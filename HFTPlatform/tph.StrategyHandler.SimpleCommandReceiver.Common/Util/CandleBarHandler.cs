using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.Util
{
    public class CandleBarHandler
    {
        #region Protected Static Consts

        protected static string _KEY_FORMAT_ = "ddMMyyyyhhmm";
        
        #endregion
        
        #region Proteted Static Attributes
        
        protected static Dictionary<string,Dictionary<string,Candlebar> > Candlebars { get; set; } 
        
        
        #endregion
        
        #region Protected Static Methods

        protected static double? GetCandlePrice(MarketData md)
        {

            if (md.Trade.HasValue)
                return md.Trade.Value;
            else if (md.GetMidPrice().HasValue)
                return md.GetMidPrice().Value;
            else
            {
                return null;
            }
        }

        protected static string ExtractDate(MarketData md, ref DateTime date)
        {
            string key = null;
            if (md.MDEntryDate.HasValue)
            {
                key = md.MDEntryDate.Value.ToString(_KEY_FORMAT_);
                date = new DateTime(md.MDEntryDate.Value.Year, md.MDEntryDate.Value.Month, md.MDEntryDate.Value.Day,
                    md.MDEntryDate.Value.Hour, md.MDEntryDate.Value.Minute, 59);
            }
            else if (md.MDLocalEntryDate.HasValue)
            {
                key = md.MDLocalEntryDate.Value.ToString(_KEY_FORMAT_);
                date = new DateTime(md.MDLocalEntryDate.Value.Year, md.MDLocalEntryDate.Value.Month, md.MDLocalEntryDate.Value.Day,
                    md.MDLocalEntryDate.Value.Hour, md.MDLocalEntryDate.Value.Minute, 59);
            }
            else
            {
                throw new Exception(string.Format("Could not find date for Market Data for Symbol {0}!",md.Security.Symbol));
            }

            return key;


        }

        #endregion
        
        #region Public Static Method

        public static void InitializeNewSubscription(Security security)
        {

            if(Candlebars==null)
                Candlebars= new Dictionary<string, Dictionary<string, Candlebar>>();


            if (!Candlebars.ContainsKey(security.Symbol))
                Candlebars.Add(security.Symbol, new Dictionary<string, Candlebar>());
            

        }

        public static Candlebar ProcessMarketData(MarketData md)
        {
            if (Candlebars.ContainsKey(md.Security.Symbol))
            {
                Dictionary<string, Candlebar> candlesDict = Candlebars[md.Security.Symbol];

                string key = null;
                DateTime date=DateTime.Now;;
                key = ExtractDate(md, ref date);

                if (candlesDict.ContainsKey(key))
                {
                    Candlebar cb = candlesDict[key];

                    double? mdPrice =  GetCandlePrice(md);
                    if ( mdPrice.HasValue)
                    {
                        if (!cb.High.HasValue || mdPrice.Value > cb.High)
                            cb.High = mdPrice;
                        if (!cb.Low.HasValue ||mdPrice.Value < cb.Low)
                            cb.Low = mdPrice;
                        if (!cb.Open.HasValue ||cb.Open.HasValue)
                            cb.Open = mdPrice;
                        cb.Trade = mdPrice;
                        cb.Close = mdPrice;
                        cb.Volume = md.NominalVolume.HasValue ? Convert.ToInt32(md.NominalVolume.Value) : 0;
                        
                    }
                    
                    return null;//Nothing new to show
                }
                else
                {
                    double? mdPrice =  GetCandlePrice(md);
                    Candlebar newCb = new Candlebar()
                    {
                        Security = md.Security,
                        Key = key,
                        Date = date,
                        High = mdPrice,
                        Low = mdPrice,
                        Open = mdPrice,
                        Trade = mdPrice,
                        Close = mdPrice,
                        Volume = md.NominalVolume.HasValue ? Convert.ToInt32(md.NominalVolume.Value) : 0
                    };
                    
                    candlesDict.Add(key, newCb);
                    
                    if (candlesDict.Values.Count > 1)
                        return candlesDict.Values.ToArray()[candlesDict.Count - 2]; //Previous candle
                    else
                        return null;
                }
            }
            else
            {
                throw new Exception(string.Format("Error processing market data for symbol {0}", md.Security.Symbol));
            }
        }
        #endregion
    }
}