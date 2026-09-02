namespace VoltStream.Application.Features.AuditLogs.Mappers;

using AutoMapper;
using VoltStream.Application.Features.AuditLogs.DTOs;
using VoltStream.Domain.Entities;

public class AuditLogMappingProfile : Profile
{
    public AuditLogMappingProfile()
        => CreateMap<AuditLog, AuditLogDto>();
}
