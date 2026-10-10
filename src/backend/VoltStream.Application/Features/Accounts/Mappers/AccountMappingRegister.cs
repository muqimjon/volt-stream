namespace VoltStream.Application.Features.Accounts.Mappers;

using Mapster;
using VoltStream.Application.Features.Accounts.DTOs;
using VoltStream.Domain.Entities;

public class AccountMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Account, AccountDto>().MapToConstructor(true);
        config.NewConfig<AccountDto, Account>();
        config.NewConfig<AccountCommandDto, Account>();
    }
}
