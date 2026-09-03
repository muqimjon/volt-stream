namespace VoltStream.Application.Features.Supplies.Mappers;

using Mapster;
using VoltStream.Application.Features.Supplies.Commands;
using VoltStream.Application.Features.Supplies.DTOs;
using VoltStream.Domain.Entities;

public class SupplyMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateSupplyCommand, Supply>()
            .Map(dest => dest.Date, src => src.Date.ToOffset(TimeSpan.Zero))
            .Ignore(dest => dest.UnitPrice)
            .Ignore(dest => dest.DiscountRate);

        config.NewConfig<UpdateSupplyCommand, Supply>()
            .Map(dest => dest.Date, src => src.Date.ToOffset(TimeSpan.Zero));

        config.NewConfig<CreateSupplyCommand, WarehouseStock>()
            .Ignore(dest => dest.UnitPrice)
            .Ignore(dest => dest.DiscountRate);

        config.NewConfig<Supply, SupplyDto>();
    }
}
