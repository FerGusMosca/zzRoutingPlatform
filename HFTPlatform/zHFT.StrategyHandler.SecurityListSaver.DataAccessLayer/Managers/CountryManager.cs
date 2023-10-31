using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.SecurityListSaver.BusinessEntities;
using zHFT.StrategyHandler.SecurityListSaver.DataAccess;

namespace zHFT.StrategyHandler.SecurityListSaver.DataAccessLayer.Managers
{
    public class CountryManager : MappingEnabledAbstract
    {
        #region Constructors

        public CountryManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(countries countryDB, Country country)
        {
            country.Code = countryDB.code;
            country.Id = countryDB.id;
            country.Name = countryDB.name;
        }

        private Country Map(countries countryDB)
        {
            Country country = new Country();
            FieldMap(countryDB, country);
            return country;
        }

        #endregion

        #region Public Methods

        public Country GetByCode(string code)
        {
            countries countryDB = ctx.countries.Where(x => x.code == code).FirstOrDefault();

            if (countryDB != null)
            {
                Country country = Map(countryDB);
                return country;
            }
            else
                return null;

        }

        #endregion
    }
}
