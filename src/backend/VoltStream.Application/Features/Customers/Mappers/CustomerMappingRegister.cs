namespace VoltStream.Application.Features.Customers.Mappers;

using Mapster;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Features.Customers.Commands;
using VoltStream.Application.Features.Customers.DTOs;
using VoltStream.Domain.Entities;

public class CustomerMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Customer, CustomerDto>();
        config.NewConfig<Customer, CustomerForOperationDto>();

        config.NewConfig<CreateCustomerCommand, Customer>()
            .Map(dest => dest.NormalizedName, src => src.Name.ToNormalized())
            .Map(dest => dest.NormalizedEmail, src => src.Email.ToNormalized());

        config.NewConfig<UpdateCustomerCommand, Customer>()
            .Map(dest => dest.NormalizedName, src => src.Name.ToNormalized())
            .Map(dest => dest.NormalizedEmail, src => src.Email.ToNormalized());
    }
}
