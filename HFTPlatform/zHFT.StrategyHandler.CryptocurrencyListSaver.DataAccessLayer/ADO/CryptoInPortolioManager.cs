using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.DataAccessLayer.Managers.ADO;
using zHFT.StrategyHandler.CryptocurrencyListSaver.BusinessEntities;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver.DataAccessLayer.ADO
{
    public class CryptoInPortolioManager : ADOBaseManager
    {

        #region Constructores

        public CryptoInPortolioManager(string connectionString)
        {
            DatabaseConnection = new SqlConnection(connectionString);
            

        }

        #endregion

        #region Private Methods


        private CryptoInPortfolio BuildCryptoInPortfolio(SqlDataReader reader)
        {
            CryptoInPortfolio crypto = new CryptoInPortfolio();

            crypto.Symbol = reader["CryptoSymbol"].ToString();
            crypto.QuoteCurrency = reader["CryptoQuoteCurrency"].ToString();
            crypto.Market = reader["CryptoMarket"].ToString();
            crypto.Country = reader["CryptoCountry"].ToString();
            crypto.Category = reader["CryptoCategory"].ToString();
            crypto.Folder = reader["CryptoFolder"].ToString();
            crypto.Exchange = reader["CryptoExchange"].ToString();
            return crypto;
        }

        #endregion

        #region Public Methods

        public List<CryptoInPortfolio> GetCryptoInPortfolio()
        {
            DatabaseConnection.Open();

            List<CryptoInPortfolio> cryptosInPortfolio = new List<CryptoInPortfolio>();

            SqlCommand cmd = new SqlCommand("GetCryptosInPortfolio", DatabaseConnection);

            cmd.CommandType = CommandType.StoredProcedure;

            SqlDataReader reader = null;
            CryptoInPortfolio crypto = null;
            try
            {
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    cryptosInPortfolio.Add(BuildCryptoInPortfolio(reader));

                }
            }
            catch
            {
                throw;

            }
            finally
            {
                reader.Close();
                DatabaseConnection.Close();
            }
            return cryptosInPortfolio;
        
        }

        #endregion
    }
}
