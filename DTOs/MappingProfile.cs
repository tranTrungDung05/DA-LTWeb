using AutoMapper;
using smart_hostel_management_system.Models.Core;

/*
---------THIS FILE WAS SUPPORTED BY AI, JUST FOR MAPPING FASTER---------
*/

namespace smart_hostel_management_system.DTOs
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Contract, HoaDonDTO>()
                .ForMember(dest => dest.RoomPrice, opt => opt.MapFrom(src => src.MonthlyPrice))
                .ForMember(dest => dest.RoomId, opt => opt.MapFrom(src => src.RoomId))
                .ForMember(dest => dest.TenantName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.FullName : "Khach thue"))
                .ForMember(dest => dest.TenantEmail, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.Email : string.Empty))
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.MonthlyPrice))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => DateTime.Now.AddDays(5)))
                .ForMember(dest => dest.Month, opt => opt.MapFrom(src => DateTime.Now.Month))
                .ForMember(dest => dest.Year, opt => opt.MapFrom(src => DateTime.Now.Year));
        }
    }
}