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
    public class StockProfile : Profile
    {
        public override string ProfileName
        {
            get { return "StockProfile"; }
        }

        protected override void Configure()
        {
            CreateMap<usa_stocks, Stock>()
                .ForMember(dest => dest.Ticker, opt => opt.MapFrom(src => src.codigo_especie))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.nombre))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.pais))
                .ForMember(dest => dest.Market, opt => opt.MapFrom(src => src.mercado))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.categoria))
                ;

            CreateMap<Stock, usa_stocks>()
                .ForMember(dest => dest.codigo_especie, opt => opt.MapFrom(src => src.Ticker))
                .ForMember(dest => dest.nombre, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.mercado, opt => opt.MapFrom(src => src.Market))
                .ForMember(dest => dest.pais, opt => opt.MapFrom(src => src.Country))
                .ForMember(dest => dest.categoria, opt => opt.MapFrom(src => src.Category))
                ;

        }
    }
}
