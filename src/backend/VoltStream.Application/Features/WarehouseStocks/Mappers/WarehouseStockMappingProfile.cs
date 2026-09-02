namespace VoltStream.Application.Features.WarehouseStocks.Mappers;

using AutoMapper;
using VoltStream.Application.Features.WarehouseStocks.DTOs;
using VoltStream.Domain.Entities;

public class WarehouseStockMappingProfile : Profile
{
    public WarehouseStockMappingProfile()
    {
        CreateMap<WarehouseStock, WarehouseStockDto>()
            .ForMember(dest => dest.BranchId,
                opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.BranchId : 0));
        CreateMap<WarehouseStock, WarehouseStockForProductDto>();
    }
}
