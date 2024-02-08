using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using tph.StrategyHandler.HistoricalPricesAnalyzer.BE;
using tph.StrategyHandler.HistoricalPricesAnalyzer.Common.Configuration;
using tph.StrategyHandler.HistoricalPricesDownloader;
using tph.StrategyHandler.HistoricalPricesDownloader.Common.Configuration;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.BusinessEntities;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.DataAccessLayer;
using static zHFT.Main.Common.Util.Constants;

namespace tph.StrategyHandler.HistoricalPricesAnalyzer
{
    public class HistoricalPricesAnalyzer: HistoricalPricesDownloader.HistoricalPricesDownloader
    {

        #region Protected Attributes

        protected override HistoricalPricesDownloaderConfiguration Config { get; set; }

        protected DateRangeClassificationManager DateRangeClassificationManager { get; set; }

        protected Dictionary<string, MonTurtlePosition> AvailableIndicators { get; set; }

        #endregion

        #region Protected Methods

        public Common.Configuration.Configuration GetConfig()
        {
            return (Common.Configuration.Configuration)Config;
        
        }

        public override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            Config = ConfigLoader.GetConfiguration<Common.Configuration.Configuration>(this, configFile, listaCamposSinValor);
        }


        protected override void RequestHistoricalPrices()
        {
            try
            {
                DateRangeClassificationManager = new DateRangeClassificationManager(GetConfig().OutputConnectionString);

                DateTime to = Config.To.Value.AddDays(1);
                DateTime from = Config.From.Value;
                SecurityType secType = Security.GetSecurityType(Config.SecurityType);
                CandleInterval interval = Config.GetCandleInterval();

                AvailableIndicators = new Dictionary<string, MonTurtlePosition>();

                Thread.Sleep(Config.PacingOnConnections);

                foreach (string symbol in Config.SymbolsToDownload)
                {
                    Security sec = EvalBuildProcessingEntities(symbol, secType, Config.Currency, Config.Exchange);

                    ProcessPrices(AvailableIndicators[symbol], interval, from, to);

                }

                

            }
            catch (Exception ex)
            {
                DoLog($"{Config.Name}--> CRITICAL ERROR fetching historical prices:{ex.Message}", MessageType.Error);
            }
        }


        protected override Security EvalBuildProcessingEntities(string symbol, SecurityType secType, string currency, string exchange)
        {
            Security sec = new Security()
            {
                Symbol = symbol,
                SecType = secType,
                Currency = currency,
                Exchange = exchange

            };

            try
            {
                DoLog($"Building indicator assembly for symbol {symbol}:{GetConfig().IndicatorAnalysisClass}", MessageType.Information);

                var indicatorCls = Type.GetType(GetConfig().IndicatorAnalysisClass);
                if (indicatorCls != null)
                {
                    object[] param = new object[] { sec, GetConfig().IndicatorConfigafile };
                    var indicatorIns = (MonTurtlePosition)Activator.CreateInstance(indicatorCls, param);
                    AvailableIndicators.Add(sec.Symbol, indicatorIns);

                    DoLog($"Assembly for symbol {symbol} successfully instantiated!", MessageType.Information);
                }
                else
                    throw new Exception($"ERROR building assembly :{GetConfig().IndicatorAnalysisClass}:Could not be found!");


                return sec;
            }
            catch(Exception ex)
            {
                
                throw new Exception($"@{GetConfig().Name}: ERROR instantiating indicator analyzer class: {ex.Message}");
                

            }

        }

        protected void ProcessPrices(MonTurtlePosition pos, CandleInterval interval,DateTime from , DateTime to)
        {
            
            try
            {
                DoLog($"Fetching candles for symbol {pos.Security.Symbol}: From={from} To={to} Interval={interval}", MessageType.Information);
                List<MarketData> candles = CandleManager.GetCandles(pos.Security.Symbol, interval, from, to);
                DoLog($"Found {candles.Count} candles . Processing candles", MessageType.Information);

                foreach (MarketData candle in candles)
                {
                    DoLog($"Processing candle for date {candle.GetReferenceDateTime()} for symbol {candle.Security.Symbol}", MessageType.Information);
                    pos.AppendCandle(candle);//This one will already calculate the project status
                }

                ((Indicator)pos).CloseLastOpenClassification(candles.OrderByDescending(x => x.GetReferenceDateTime().Value).FirstOrDefault());

                List<DateRangeClassification> indicatorClassifications = ((Indicator)pos).GetDateRangeClassifications();

                DoLog($"Persisting {indicatorClassifications.Count} ranges for symbol {pos.Security.Symbol}", MessageType.Information);

                foreach (DateRangeClassification drc in indicatorClassifications)
                {
                    DoLog($"Persisting range from {drc.DateStart} to {drc.DateEnd} --> classif={drc.Classification}", MessageType.Information);
                    DateRangeClassificationManager.Persist(drc);
                }
                DoLog($"{indicatorClassifications.Count} range classifications successfully persisted", MessageType.Information);

                DoLog($"{candles.Count} successfully processed", MessageType.Information);
            }
            catch (Exception ex)
            {
                DoLog($"CRITICAL ERROR processing candles for symbol {pos.Security.Symbol}:{ex.Message}",MessageType.Error);
            }
        
        }

        #endregion

    }
}
