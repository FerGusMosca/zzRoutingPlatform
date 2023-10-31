using MomentumBackTests.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.Cryptos.DataAccess;



namespace zHFT.InstructionBasedMarketClient.Cryptos.DataAccessLayer
{
    public class MappingEnabledAbstract
    {
        #region Protected Methods
        protected readonly AutPortfolioEntities ctx;
        #endregion

        #region Constructors
        public MappingEnabledAbstract(AutPortfolioEntities context)
        {
            ctx = context;
            //AutoMapperConfiguration.Instance.Configure();
        }



        public MappingEnabledAbstract(string connectionString)
        {
            ctx = DataContextFactory.GetAutPortfolioDataContext(connectionString);
            //AutoMapperConfiguration.Instance.Configure();
        }


        #endregion
    }
}
