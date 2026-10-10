namespace VoltStream.Application.Features.Sales.Mappers;

using Mapster;
using VoltStream.Application.Features.Sales.Commands;
using VoltStream.Application.Features.Sales.DTOs;
using VoltStream.Domain.Entities;

public class SaleMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateSaleCommand, Sale>()
            .Map(dest => dest.Date, src => src.Date.ToOffset(TimeSpan.Zero));

        config.NewConfig<UpdateSaleCommand, Sale>()
            .Map(dest => dest.Date, src => src.Date.ToOffset(TimeSpan.Zero));

        config.NewConfig<CreateSaleCommand, CustomerOperation>()
            .Ignore(dest => dest.Id);

        config.NewConfig<Sale, SaleDto>();
        config.NewConfig<Sale, SaleForCustomerOperationDto>();

        config.NewConfig<SaleItem, SaleItemDto>();
        config.NewConfig<SaleItemCommand, SaleItem>();
    }
}
