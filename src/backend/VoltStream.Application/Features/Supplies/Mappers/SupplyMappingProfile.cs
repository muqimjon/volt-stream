namespace VoltStream.Application.Features.Supplies.Mappers;

using AutoMapper;
using VoltStream.Application.Features.Supplies.Commands;
using VoltStream.Application.Features.Supplies.DTOs;
using VoltStream.Domain.Entities;

public class SupplyMappingProfile : Profile
{
    public SupplyMappingProfile()
    {
        CreateMap<CreateSupplyCommand, Supply>()
            .ForMember(dest => dest.UnitPrice, opt => opt.Ignore())
            .ForMember(dest => dest.DiscountRate, opt => opt.Ignore());
        CreateMap<UpdateSupplyCommand, Supply>();
        CreateMap<CreateSupplyCommand, WarehouseStock>()
            .ForMember(dest => dest.UnitPrice, opt => opt.Ignore())
            .ForMember(dest => dest.DiscountRate, opt => opt.Ignore());
        CreateMap<Supply, SupplyDto>();
    }
}
