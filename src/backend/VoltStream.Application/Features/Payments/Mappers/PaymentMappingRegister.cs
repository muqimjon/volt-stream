namespace VoltStream.Application.Features.Payments.Mappers;

using Mapster;
using VoltStream.Application.Features.Payments.Commands;
using VoltStream.Application.Features.Payments.DTOs;
using VoltStream.Domain.Entities;

public class PaymentMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreatePaymentCommand, Payment>()
            .Map(dest => dest.PaidAt, src => src.PaidAt.ToUniversalTime());

        config.NewConfig<UpdatePaymentCommand, Payment>()
            .Map(dest => dest.PaidAt, src => src.PaidAt.ToUniversalTime());

        config.NewConfig<CreatePaymentCommand, CustomerOperation>()
            .Map(dest => dest.Amount, src => src.NetAmount);

        config.NewConfig<UpdatePaymentCommand, CustomerOperation>()
            .Map(dest => dest.Amount, src => src.NetAmount);

        config.NewConfig<Payment, PaymentDto>();
        config.NewConfig<Payment, PaymentForCustomerOperationDto>();
    }
}
