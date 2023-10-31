using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.MomentumPortfolios.BusinessEntities;
using zHFT.StrategyHandler.MomentumPortfolios.DataAccess;

namespace zHFT.StrategyHandler.MomentumPortfolios.DataAccessLayer.Profiles
{
    public class ConfigurationProfile : Profile
    {
        public override string ProfileName
        {
            get { return "ConfigurationProfile"; }
        }

        protected override void Configure()
        {
            CreateMap<configuraciones, Configuration>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.fecha_inicio))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.fecha_fin))
                .ForMember(dest => dest.FormationMonths, opt => opt.MapFrom(src => src.meses_formacion))
                .ForMember(dest => dest.HoldingMomths, opt => opt.MapFrom(src => src.meses_tenencia))
                .ForMember(dest => dest.SkippingMonths, opt => opt.MapFrom(src => src.meses_skip))
                .ForMember(dest => dest.StocksInPortfolio, opt => opt.MapFrom(src => src.acciones_portfolo))
                .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => Configuration.GetWeight(src.peso)))
                .ForMember(dest => dest.TxCosts, opt => opt.MapFrom(src => src.costos_por_tx))
                .ForMember(dest => dest.ThresholdBigCap, opt => opt.MapFrom(src => src.threshold_big_cap))
                .ForMember(dest => dest.ThresholdMediumCap, opt => opt.MapFrom(src => src.threshold_medium_cap))
                .ForMember(dest => dest.FilterStocks, opt => opt.MapFrom(src => Configuration.GetFilterStocks(src.filtro_acciones)))
                .ForMember(dest => dest.RankingRatio, opt => opt.MapFrom(src => Configuration.GetRankingRatio(src.ratio_valorizacion)))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.pais))
                ;

            CreateMap<Configuration, configuraciones>()
                //.ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.fecha_inicio, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.fecha_fin, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.meses_formacion, opt => opt.MapFrom(src => src.FormationMonths))
                .ForMember(dest => dest.meses_tenencia, opt => opt.MapFrom(src => src.HoldingMomths))
                .ForMember(dest => dest.meses_skip, opt => opt.MapFrom(src => src.SkippingMonths))
                .ForMember(dest => dest.acciones_portfolo, opt => opt.MapFrom(src => src.StocksInPortfolio))
                .ForMember(dest => dest.peso, opt => opt.MapFrom(src => src.Weight.ToString()))
                .ForMember(dest => dest.costos_por_tx, opt => opt.MapFrom(src => src.TxCosts))
                .ForMember(dest => dest.threshold_big_cap, opt => opt.MapFrom(src => src.ThresholdBigCap))
                .ForMember(dest => dest.threshold_medium_cap, opt => opt.MapFrom(src => src.ThresholdMediumCap))
                .ForMember(dest => dest.filtro_acciones, opt => opt.MapFrom(src => src.FilterStocks.ToString()))
                .ForMember(dest => dest.ratio_valorizacion, opt => opt.MapFrom(src => src.RankingRatio.ToString()))
                .ForMember(dest => dest.pais, opt => opt.MapFrom(src => src.Country))
                ;
        }
    }
}
