namespace VoltStream.Application.Features.CustomerOperations.Mappers;

using Mapster;
using VoltStream.Application.Features.CustomerOperations.DTOs;
using VoltStream.Domain.Entities;

public class CustomerOperationMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CustomerOperation, CustomerOperationDto>();
        config.NewConfig<CustomerOperation, CustomerOperationForPaymentDto>();
        config.NewConfig<CustomerOperation, CustomerOperationForSaleDto>();
    }
}
