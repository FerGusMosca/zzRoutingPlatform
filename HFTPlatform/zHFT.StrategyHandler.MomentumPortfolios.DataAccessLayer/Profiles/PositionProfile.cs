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
    public class PositionProfile : Profile
    {
        public override string ProfileName
        {
            get { return "PositionProfile"; }
        }

        protected override void Configure()
        {
            CreateMap<posiciones, Position>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.usa_stocks))
                .ForMember(dest => dest.WinnerRatio, opt => opt.MapFrom(src => src.ratio_ganador))
                .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.peso))
                .ForMember(dest => dest.MarketCap, opt => opt.MapFrom(src => src.market_cap))
                .ForMember(dest => dest.Portfolio, opt => opt.MapFrom(src => src.portfolios))
                .ForMember(dest => dest.Processed, opt => opt.MapFrom(src => src.procesado))
                ;

            CreateMap<Position, posiciones>()
                //.ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.codigo_especie, opt => opt.MapFrom(src => src.Stock.Ticker))
                .ForMember(dest => dest.ratio_ganador, opt => opt.MapFrom(src => src.WinnerRatio))
                .ForMember(dest => dest.peso, opt => opt.MapFrom(src => src.Weight))
                .ForMember(dest => dest.market_cap, opt => opt.MapFrom(src => src.MarketCap))
                .ForMember(dest => dest.portfolios, opt => opt.MapFrom(src => src.Portfolio))
                ;

        }
    }
}
