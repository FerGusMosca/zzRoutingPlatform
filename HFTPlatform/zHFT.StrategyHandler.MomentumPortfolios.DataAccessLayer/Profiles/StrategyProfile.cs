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
    public class StrategyProfile : Profile
    {
        public override string ProfileName
        {
            get { return "StrategyProfile"; }
        }

        protected override void Configure()
        {
            CreateMap<estrategias, Strategy>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.fecha))
                .ForMember(dest => dest.Configuration, opt => opt.MapFrom(src => src.configuraciones))
                .ForMember(dest => dest.Portfolios, opt => opt.MapFrom(src => src.portfolios))
                ;

            CreateMap<Strategy, estrategias>()
                //.ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.fecha, opt => opt.MapFrom(src => src.Date))
                .ForMember(dest => dest.configuraciones, opt => opt.MapFrom(src => src.Configuration))
                .ForMember(dest => dest.portfolios, opt => opt.MapFrom(src => src.Portfolios))
                ;

        }
    }
}
