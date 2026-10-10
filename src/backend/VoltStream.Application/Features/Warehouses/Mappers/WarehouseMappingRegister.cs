namespace VoltStream.Application.Features.Warehouses.Mappers;

using Mapster;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Features.Warehouses.Commands;
using VoltStream.Application.Features.Warehouses.DTOs;
using VoltStream.Domain.Entities;

public class WarehouseMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Warehouse, WarehouseDto>()
            .MapToConstructor(true)
            .Map(dest => dest.Stocks, src => src.Stocks ?? new List<WarehouseStock>());

        config.NewConfig<CreateWarehouseCommand, Warehouse>()
            .Map(dest => dest.NormalizedName, src => src.Name.ToNormalized());

        config.NewConfig<UpdateWarehouseCommand, Warehouse>()
            .Map(dest => dest.NormalizedName, src => src.Name.ToNormalized());
    }
}
