using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.EntityClient;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.StrategyHandler.OptionsContractSaver.BusinessEntities;
using zHFT.StrategyHandler.OptionsContractSaver.DataAccess;

namespace zHFT.StrategyHandler.OptionsContractSaver.DataAccessLayer.Managers
{
    public class OptionBidManager : MappingEnabledAbstract
    {
        #region Protected Attributes

        public string ADOConnectionString { get; set; }

        public SqlConnection Connection { get; set; }

        #endregion


        #region Constructors
        public OptionBidManager(AutPortfolioEntities context) : base(context) { }

        public OptionBidManager(string efConnectionString,string adoConnectionString)
            : base(efConnectionString)
        {
            ADOConnectionString = adoConnectionString;
            
        }
        #endregion

        #region Protected Methods

        private void FieldMap(OptionBid optionBid, option_bids optionBidDB)
        {
            optionBidDB.symbol = optionBid.Option.Symbol;
            optionBidDB.option_id = optionBid.Option.Id;
            optionBidDB.timestamp = optionBid.Timestamp;
            optionBidDB.size = optionBid.Size;
            optionBidDB.price = optionBid.Price;
            optionBidDB.side = Convert.ToChar(optionBid.Side).ToString();
            optionBidDB.underlying_price = optionBid.UnderlyingPrice;

        }

        private void FieldMap(option_bids optionBidDB, OptionBid optionBid)
        {
            optionBid.Option = new Option()
                {
                    Symbol = optionBidDB.options.symbol,
                    SymbolSfx = optionBidDB.options.symbol_sfx,
                    StrikePrice = optionBidDB.options.strike_price,
                    MaturityDate = optionBidDB.options.maturity_date,
                    MaturityMonthYear = optionBidDB.options.maturity_month_year,
                    PutOrCall = (PutOrCall)Convert.ToChar(optionBidDB.options.put_or_call),
                    StrikeMultiplier = optionBidDB.options.strike_multiplier,
                    StrikeCurrency = optionBidDB.options.strike_currency,
                    Currency = optionBidDB.options.currency,
                    SecurityExchange = optionBidDB.options.security_exchange,
                    Expired = optionBidDB.options.expired
                };

            optionBid.Timestamp = optionBidDB.timestamp;
            optionBid.Size = optionBidDB.size;
            optionBid.Price = optionBidDB.price;
            optionBid.Side = (Side) Convert.ToChar(optionBidDB.side);
            optionBid.UnderlyingPrice = optionBidDB.underlying_price;
        }

        private option_bids Map(OptionBid optionBid)
        {
            option_bids optionBidDB = new option_bids();
            FieldMap(optionBid, optionBidDB);
            return optionBidDB;
        }

        private OptionBid Map(option_bids optionBidDB)
        {
            OptionBid optionBid = new OptionBid();
            FieldMap(optionBidDB, optionBid);
            return optionBid;
        }

        #endregion

        #region Public Methods

        public OptionBid GetOptionBid(string symbol,Side side)
        {
            string strSide=Convert.ToChar(side).ToString();
            option_bids optionBidDB = ctx.option_bids.Where(x => x.options.symbol == symbol
                                                                 && x.side == strSide
                                                                 && !x.options.expired).FirstOrDefault();

           if (optionBidDB != null)
               return Map(optionBidDB);
           else
               return null;
        }

       
        public void Persist(OptionBid bid)
        {
            using (var connection = new SqlConnection(ADOConnectionString))
            {
                using (SqlCommand cmd = connection.CreateCommand())
                {
                    connection.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "persist_option_bids";
                    cmd.Parameters.Add(new SqlParameter("@Symbol", bid.Option.Symbol));
                    cmd.Parameters.Add(new SqlParameter("@date", bid.Timestamp));
                    cmd.Parameters.Add(new SqlParameter("@size", bid.Size));
                    cmd.Parameters.Add(new SqlParameter("@side", Convert.ToChar(bid.Side)));
                    cmd.Parameters.Add(new SqlParameter("@price", bid.Price));
                    cmd.Parameters.Add(new SqlParameter("@underlying_price", bid.UnderlyingPrice));
                    cmd.Parameters.Add(new SqlParameter("@option_id", bid.Option.Id));

                    cmd.ExecuteNonQuery();
                }
                connection.Dispose();
            }
        }

        #endregion
    }
}
