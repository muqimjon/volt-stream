namespace VoltStream.Application.Features.Branches.Commands;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsBranches)]
public record CreateBranchCommand(string Name) : IRequest<long>;

public class CreateBranchCommandHandler(IAppDbContext context)
    : IRequestHandler<CreateBranchCommand, long>
{
    public async Task<long> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.ToNormalized();

        if (await context.Branches.AnyAsync(b => b.NormalizedName == normalizedName, cancellationToken))
            throw new AlreadyExistException(nameof(Branch), nameof(request.Name), request.Name);

        var branch = new Branch
        {
            Name = request.Name.Trim(),
            NormalizedName = normalizedName,
            Warehouses =
            [
                new Warehouse
                {
                    Name = request.Name.Trim(),
                    NormalizedName = normalizedName
                }
            ]
        };

        context.Branches.Add(branch);
        await context.SaveAsync(cancellationToken);

        return branch.Id;
    }
}
