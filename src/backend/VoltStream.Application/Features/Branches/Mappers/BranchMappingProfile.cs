namespace VoltStream.Application.Features.Branches.Mappers;

using AutoMapper;
using VoltStream.Application.Features.Branches.DTOs;
using VoltStream.Domain.Entities;

public class BranchMappingProfile : Profile
{
    public BranchMappingProfile()
    {
        CreateMap<Branch, BranchDto>();
    }
}
