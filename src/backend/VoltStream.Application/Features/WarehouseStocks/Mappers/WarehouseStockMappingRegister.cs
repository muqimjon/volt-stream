namespace VoltStream.Application.Features.WarehouseStocks.Mappers;

using Mapster;
using VoltStream.Application.Features.WarehouseStocks.DTOs;
using VoltStream.Domain.Entities;

public class WarehouseStockMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<WarehouseStock, WarehouseStockDto>()
            .Map(dest => dest.BranchId, src => src.Warehouse != null ? src.Warehouse.BranchId : 0);

        config.NewConfig<WarehouseStock, WarehouseStockForProductDto>();
    }
}
