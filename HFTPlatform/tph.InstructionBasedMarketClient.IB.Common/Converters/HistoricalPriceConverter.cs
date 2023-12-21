using System;
using tph.InstructionBasedMarketClient.IB.Common.DTO;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Wrappers;

namespace tph.InstructionBasedMarketClient.IB.Common.Converters
{
    public class HistoricalPriceConverter: ConverterBase
    {
        #region  Private Static Consts

        protected static string _DEF_CURRENCY = "USD";

        protected static SecurityType _DEF_SECURITY_TYPE = SecurityType.CS;


        protected static int _DEFAULT_HISTORY_VALUE = -2;

        #endregion
        

        #region  Protected Statuc Methods

        protected static string GetBarSize(CandleInterval interval)
        {
            if (interval == CandleInterval.Minute_1)
                return HistoricalPricesRequestDTO._INT_1_MIN;
            else if (interval == CandleInterval.Minute_5)
                return HistoricalPricesRequestDTO._INT_5_MIN;
            else if (interval == CandleInterval.HOUR_1)
                return HistoricalPricesRequestDTO._INT_1_HOUR;
            else if (interval == CandleInterval.HOUR_5)
                return HistoricalPricesRequestDTO._INT_5_HOUR;
            else if (interval == CandleInterval.DAY)
                return HistoricalPricesRequestDTO._INT_DAY;
            else
                throw new Exception($"Interval not found {interval}");

        }

        protected static string GetQueryTime(DateTime?  to)
        {
            if (to.HasValue)
            {
                return to.Value.ToString(HistoricalPricesRequestDTO._QUERY_TIME_DATEFORMAT);
            }
            else//2 days default
            {
                return DateTime.Now.AddDays(_DEFAULT_HISTORY_VALUE).ToString(HistoricalPricesRequestDTO._QUERY_TIME_DATEFORMAT);
            }
        }

        protected static string GetDuration(DateTime? from, DateTime? to)
        {
            
            if(!to.HasValue)
                to=DateTime.Now;


            if (!from.HasValue)
                from = to.Value.AddDays(_DEFAULT_HISTORY_VALUE);


            TimeSpan elapsed = to.Value - from.Value;

            if (elapsed.TotalDays > 0)
                return $"{Convert.ToInt32(elapsed.TotalDays)} D";
            else if (elapsed.TotalHours > 0)
                return $"{Convert.ToInt32(elapsed.TotalHours)} H";
            else if (elapsed.TotalMinutes > 0)
                return $"{Convert.ToInt32(elapsed.TotalHours)} M";
            else if (elapsed.TotalSeconds > 0)
                return $"{Convert.ToInt32(elapsed.TotalHours)} S";
            else
            {
                throw new Exception($"Invalid date range : from={from.Value} to={to.Value}");
            }
        }

        #endregion
        
        #region Public Static Methods

        public static HistoricalPricesRequestDTO ConvertHistoricalPriceRequest(Wrapper wrapper)
        {
            if (wrapper is HistoricalPricesRequestWrapper)
            {
                HistoricalPricesRequestDTO dto = new HistoricalPricesRequestDTO();
                
                if (ValidateField(wrapper, HistoricalPricesRequestFields.MDReqId))
                    dto.ReqId=(int) wrapper.GetField(HistoricalPricesRequestFields.MDReqId);
                else
                    throw new Exception($"Missing mandatory field MDReqId for historical data request");

                if (ValidateField(wrapper, HistoricalPricesRequestFields.Symbol))
                    dto.Symbol = (string) wrapper.GetField(HistoricalPricesRequestFields.Symbol);
                else
                    throw new Exception($"Missing mandatory field symbol for historical data request");


                if (ValidateField(wrapper, HistoricalPricesRequestFields.Interval))
                    dto.BarSize =
                        GetBarSize((CandleInterval) wrapper.GetField(HistoricalPricesRequestFields.Interval));
                else
                    dto.BarSize = HistoricalPricesRequestDTO._INT_1_MIN;

                if (ValidateField(wrapper, HistoricalPricesRequestFields.Currency))
                    dto.Currency = (string) wrapper.GetField(HistoricalPricesRequestFields.Currency);
                else
                    dto.Currency = _DEF_CURRENCY;
                
                
                if (ValidateField(wrapper, HistoricalPricesRequestFields.SecurityType))
                    dto.SecurityType = (SecurityType) wrapper.GetField(HistoricalPricesRequestFields.SecurityType);
                else
                    dto.SecurityType = _DEF_SECURITY_TYPE;

                if (ValidateField(wrapper, HistoricalPricesRequestFields.Exchange))
                    dto.Exchange = (string)wrapper.GetField(HistoricalPricesRequestFields.Exchange);
                else
                    dto.Exchange = null;


                DateTime? from = null;
                DateTime? to = null;
                if (ValidateField(wrapper, HistoricalPricesRequestFields.From))
                    from = (DateTime?) wrapper.GetField(HistoricalPricesRequestFields.From);
                
                if (ValidateField(wrapper, HistoricalPricesRequestFields.To))
                    to = (DateTime?) wrapper.GetField(HistoricalPricesRequestFields.To);

                dto.QueryTime = GetQueryTime(to);
                dto.WhatToShow = HistoricalPricesRequestDTO._TRADES;
                dto.DurationString = GetDuration(from, to);
                
                return dto;
            }
            else
            {
                throw new Exception(
                    $"Invalid class to request historical classes @IB--> must be  HistoricalPricesRequestWrapper");
            }


        }

        #endregion
    }
}