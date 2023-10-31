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
    public class PortfolioProfile : Profile
    {
        public override string ProfileName
        {
            get { return "PortfolioProfile"; }
        }

        protected override void Configure()
        {
            CreateMap<portfolios, Portfolio>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
                .ForMember(dest => dest.ProcessingStartingDate, opt => opt.MapFrom(src => src.fecha_inicio_calculo))
                .ForMember(dest => dest.ProcessingEndingDate, opt => opt.MapFrom(src => src.fecha_fin_calculo))
                .ForMember(dest => dest.Strategy, opt => opt.MapFrom(src => src.estrategias))
                .ForMember(dest => dest.Alarms, opt => opt.MapFrom(src => src.alarmas))
                .ForMember(dest => dest.Positions, opt => opt.MapFrom(src => src.posiciones))
                ;

            CreateMap<Portfolio, portfolios>()
                //.ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.fecha_inicio_calculo, opt => opt.MapFrom(src => src.ProcessingStartingDate))
                .ForMember(dest => dest.fecha_fin_calculo, opt => opt.MapFrom(src => src.ProcessingEndingDate))
                .ForMember(dest => dest.id_estrategia, opt => opt.MapFrom(src => src.Strategy.Id))
                .ForMember(dest => dest.posiciones, opt => opt.MapFrom(src => src.Positions))
                .ForMember(dest => dest.alarmas, opt => opt.MapFrom(src => src.Alarms))
                ;

        }
    }
}
