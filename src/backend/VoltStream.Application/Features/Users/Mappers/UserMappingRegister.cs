namespace VoltStream.Application.Features.Users.Mappers;

using Mapster;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Features.Users.Commands;
using VoltStream.Application.Features.Users.DTOs;
using VoltStream.Domain.Entities;

public class UserMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateUserCommand, User>()
            .Ignore(dest => dest.Permissions)
            .Map(dest => dest.NormalizedEmail, src => src.Email.ToNormalized())
            .Map(dest => dest.NormalizedName, src => src.Name.ToNormalized())
            .Map(dest => dest.NormalizedUsername, src => src.Username.ToNormalized())
            .Map(dest => dest.DateOfBirth, src => src.DateOfBirth.HasValue
                ? src.DateOfBirth.Value.ToUniversalTime()
                : (DateTime?)null);

        config.NewConfig<User, UserDto>()
            .MapToConstructor(true)
            .Map(dest => dest.BranchName, src => src.Branch != null ? src.Branch.Name : null);
    }
}
