namespace VoltStream.Application.Features.Branches.Mappers;

using Mapster;
using VoltStream.Application.Features.Branches.DTOs;
using VoltStream.Domain.Entities;

public class BranchMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
        => config.NewConfig<Branch, BranchDto>();
}
