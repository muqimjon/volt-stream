namespace VoltStream.Application.Features.Monitoring.Mappers;

using Mapster;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Features.Monitoring.Commands;
using VoltStream.Application.Features.Monitoring.DTOs;
using VoltStream.Domain.Entities;

public class AllowedClientMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateAllowedClientCommand, AllowedClient>()
            .Map(dest => dest.NormalizedName, src => src.DeviceName!.ToNormalized());

        config.NewConfig<UpdateAllowedClientCommand, AllowedClient>()
            .Map(dest => dest.NormalizedName, src => src.DeviceName!.ToNormalized());

        config.NewConfig<AllowedClient, AllowedClientDto>();
    }
}
