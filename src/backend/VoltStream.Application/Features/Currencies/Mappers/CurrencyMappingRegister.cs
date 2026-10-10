namespace VoltStream.Application.Features.Currencies.Mappers;

using Mapster;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Features.Currencies.Commands;
using VoltStream.Application.Features.Currencies.DTOs;
using VoltStream.Domain.Entities;

public class CurrencyMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateCurrencyCommand, Currency>()
            .Map(dest => dest.NormalizedName, src => src.Name.ToNormalized());

        config.NewConfig<UpdateCurrencyCommand, Currency>()
            .Map(dest => dest.NormalizedName, src => src.Name.ToNormalized());

        config.NewConfig<CurrencyCommand, Currency>()
            .Map(dest => dest.NormalizedName, src => src.Name.ToNormalized());

        config.NewConfig<Currency, CurrencyDto>();
    }
}
