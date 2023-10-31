using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.MomentumPortfolios.BusinessEntities;
using zHFT.StrategyHandler.MomentumPortfolios.Common.Enums;
using zHFT.StrategyHandler.MomentumPortfolios.DataAccess;
using zHFT.StrategyHandler.MomentumPortfolios.DataAccessLayer.Helpers;

namespace zHFT.StrategyHandler.MomentumPortfolios.DataAccessLayer.Managers
{
    public class StrategyManager : MappingEnabledAbstract
    {
        #region Constructors
        
        public StrategyManager(string connectionString): base(connectionString)
        {

        }
        #endregion

        #region Public Methods

        public Strategy GetLatestStrategy()
        {
            var estrategiaDB = ctx.estrategias
                            .OrderByDescending(x=>x.id)
                            .FirstOrDefault();

            return estrategiaDB.Map();
        }

        public Strategy GetStrategyByPortfolio(int portfolioId)
        {
            var portfolioDB = ctx.portfolios
                            .Where(x=>x.id==portfolioId)
                            .FirstOrDefault();

            return portfolioDB.estrategias.Map();
        }

        public Strategy GetStrategyByConfig(DateTime endDate,int stocksInPortfolio,
                                            int holdingMonths,Weight? weight,FilterStocks? filterStocks,
                                            Ratio? ratio)
        {
            string strWeight = weight.HasValue ? weight.Value.ToString() : "";
            string strFilterStocks = filterStocks.HasValue ? filterStocks.Value.ToString() : "";
            string strRatio = ratio.HasValue ? ratio.Value.ToString() : "";

            var estrategiaDB = ctx.estrategias
                            .Where(x =>
                                           x.configuraciones.acciones_portfolo == stocksInPortfolio
                                        && x.configuraciones.meses_tenencia == holdingMonths
                                        && (weight.HasValue ? x.configuraciones.peso == strWeight : true)
                                        && (filterStocks.HasValue ? x.configuraciones.filtro_acciones == strFilterStocks : true)
                                        && (ratio.HasValue ? x.configuraciones.ratio_valorizacion == strRatio : true)
                                        && (x.configuraciones.fecha_fin.Year == endDate.Year && x.configuraciones.fecha_fin.Month == endDate.Month && x.configuraciones.fecha_fin.Day == endDate.Day)
                                                            )
                            .OrderByDescending(x => x.id)
                            .FirstOrDefault();

            return estrategiaDB.Map();
        }

        #endregion


    }
}
