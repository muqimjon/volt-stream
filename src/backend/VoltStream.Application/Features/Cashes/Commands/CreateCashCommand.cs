namespace VoltStream.Application.Features.Cashes.Commands;

using MapsterMapper;
using MediatR;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;
using VoltStream.Application.Commons.Security;

[RequirePermission(Permissions.PaymentsManage)]
public record CreateCashCommand(
    decimal Balance,
    long CurrencyId)
    : IRequest<long>;

public class CreateCashCommandHandler(
    IAppDbContext context,
    IMapper mapper) : IRequestHandler<CreateCashCommand, long>
{
    public async Task<long> Handle(CreateCashCommand request, CancellationToken cancellationToken)
    {
        var cash = mapper.Map<Cash>(request);
        context.Cashes.Add(cash);
        await context.SaveAsync(cancellationToken);
        return cash.Id;
    }
}