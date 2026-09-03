namespace VoltStream.Application.Features.AuditLogs.Mappers;

using Mapster;
using VoltStream.Application.Features.AuditLogs.DTOs;
using VoltStream.Domain.Entities;

public class AuditLogMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
        => config.NewConfig<AuditLog, AuditLogDto>();
}
