namespace VoltStream.Application.Features.Users.Mappers;

using AutoMapper;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Features.Users.Commands;
using VoltStream.Application.Features.Users.DTOs;
using VoltStream.Domain.Entities;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<CreateUserCommand, User>()
            .ForMember(dest => dest.Permissions, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedEmail, opt => opt.MapFrom(src => src.Email.ToNormalized()))
            .ForMember(dest => dest.NormalizedName, opt => opt.MapFrom(src => src.Name.ToNormalized()))
            .ForMember(dest => dest.NormalizedUsername, opt => opt.MapFrom(src => src.Username.ToNormalized()))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth.HasValue
                                        ? src.DateOfBirth.Value.ToUniversalTime()
                                        : (DateTime?)null));

        CreateMap<User, UserDto>()
            .ForCtorParam(nameof(UserDto.BranchName),
                opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : null));
    }
}
