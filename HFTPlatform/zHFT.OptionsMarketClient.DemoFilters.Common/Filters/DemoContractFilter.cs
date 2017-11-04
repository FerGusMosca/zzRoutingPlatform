using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.OptionsMarketClient.BusinessEntities;
using zHFT.OptionsMarketClient.Common.Configuration;
using zHFT.OptionsMarketClient.Common.Interfaces;

namespace zHFT.OptionsMarketClient.DemoFilters.Common.Filters
{
    public class DemoContractFilter : IContractFilter
    {
        public bool ValidContract(Option option, Configuration config)
        {
            //We only wan to consider the contracts with more than <MinMaturityDistanceInMonths> months to expiration
            if (config.MinMaturityDistanceInMonths >= 0)
            {
                DateTime minDate = DateTime.Now.AddMonths(config.MinMaturityDistanceInMonths);
                DateTime maxDate = DateTime.Now.AddMonths(config.MaxMaturityDistanceInMonths);

                if (DateTime.Compare(minDate, option.MaturityDate) > 0
                   && DateTime.Compare(option.MaturityDate.Date, maxDate) > 0)
                    return false;

            }

            //We only want certain type of contracts
            if (config.PutOrCall == zHFT.OptionsMarketClient.Common.Configuration.PutOrCall.CALL && option.PutOrCall == zHFT.OptionsMarketClient.BusinessEntities.PutOrCall.Put)
                return false;
            else if (config.PutOrCall == zHFT.OptionsMarketClient.Common.Configuration.PutOrCall.PUT && option.PutOrCall == zHFT.OptionsMarketClient.BusinessEntities.PutOrCall.Call)
                return false;

            return true; 
        }

        public List<Option> FilterContracts(Security security, List<Option> options, Configuration config)
        {
            List<Option> filteredOptions = new List<Option>();

            if(security.MarketData== null  || !security.MarketData.ClosingPrice.HasValue)
                throw new Exception(string.Format("Could not find market data for security {0}",security.Symbol));


            //We filter the kind of strike price we want to use
            if (config.StrikeFilter == StrikeFilter.ITM && security.MarketData.ClosingPrice.HasValue)
                filteredOptions = options.Where(x => x.StrikePrice <= security.MarketData.ClosingPrice.Value).ToList();
            else if (config.StrikeFilter == StrikeFilter.OTM  && security.MarketData.ClosingPrice.HasValue)
                filteredOptions = options.Where(x => x.StrikePrice >= security.MarketData.ClosingPrice.Value).ToList();
            else
                filteredOptions = options;

            //We only wan to consider the contracts with more than <MinMaturityDistanceInMonths> months to expiration
            if (config.MinMaturityDistanceInMonths>=0)
            {
                DateTime minDate = DateTime.Now.AddMonths(config.MinMaturityDistanceInMonths);
                DateTime maxDate = DateTime.Now.AddMonths(config.MaxMaturityDistanceInMonths);

                filteredOptions = filteredOptions.Where(x =>     DateTime.Compare(minDate,x.MaturityDate)<=0
                                                             &&  DateTime.Compare(x.MaturityDate.Date,maxDate)<=0
                                                       ).ToList();
            
            }

            //We only want certain type of contracts
            if (config.PutOrCall == zHFT.OptionsMarketClient.Common.Configuration.PutOrCall.CALL)
                filteredOptions = filteredOptions.Where(x =>x.PutOrCall==zHFT.OptionsMarketClient.BusinessEntities.PutOrCall.Call).ToList();
            else if (config.PutOrCall == zHFT.OptionsMarketClient.Common.Configuration.PutOrCall.PUT)
                filteredOptions = filteredOptions.Where(x => x.PutOrCall == zHFT.OptionsMarketClient.BusinessEntities.PutOrCall.Put).ToList();
            else
                filteredOptions = options;


            //We want the highest <MaxContractsPerSecurity> volume options
            filteredOptions = filteredOptions.Where(x=>x.TradeVolume.HasValue).OrderByDescending(x => x.TradeVolume.Value).ToList();

            return filteredOptions;
        
        }
    }
}
