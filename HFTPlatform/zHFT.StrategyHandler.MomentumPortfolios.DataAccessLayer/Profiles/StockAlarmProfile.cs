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
    public class StockAlarmProfile : Profile
    {
        public override string ProfileName
        {
            get { return "StockAlarmProfile"; }
        }

        protected override void Configure()
        {
            CreateMap<alarmas, StockAlarm>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => src.usa_stocks))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.fecha_inicio))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.fecha_fin))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.descripcion))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => StockAlarm.GetAlarmType(src.tipo_alarma)))
                ;

            CreateMap<StockAlarm, alarmas>()
                //.ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.codigo_especie, opt => opt.MapFrom(src => src.Stock.Ticker))
                .ForMember(dest => dest.fecha_inicio, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.fecha_fin, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.descripcion, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.tipo_alarma, opt => opt.MapFrom(src => src.Type.ToString()))
                ;

        }
    }
}
