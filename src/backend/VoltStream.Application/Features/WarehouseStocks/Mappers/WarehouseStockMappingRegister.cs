namespace VoltStream.Application.Features.WarehouseStocks.Mappers;

using Mapster;
using VoltStream.Application.Features.WarehouseStocks.DTOs;
using VoltStream.Domain.Entities;

public class WarehouseStockMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<WarehouseStock, WarehouseStockDto>();

        config.NewConfig<WarehouseStock, WarehouseStockForProductDto>();
    }
}
