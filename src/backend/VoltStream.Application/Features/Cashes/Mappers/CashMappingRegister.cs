namespace VoltStream.Application.Features.Cashes.Mappers;

using Mapster;
using VoltStream.Application.Features.Cashes.Commands;
using VoltStream.Application.Features.Cashes.DTOs;
using VoltStream.Domain.Entities;

public class CashMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateCashCommand, Cash>();
        config.NewConfig<Cash, CashDto>();
    }
}
