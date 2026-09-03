namespace VoltStream.Application.Features.Branches.Commands;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsBranches)]
public record UpdateBranchCommand(long Id, string Name, bool IsActive) : IRequest<bool>;

public class UpdateBranchCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateBranchCommand, bool>
{
    public async Task<bool> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await context.Branches.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), nameof(request.Id), request.Id);

        var normalizedName = request.Name.ToNormalized();

        if (await context.Branches.AnyAsync(b => b.NormalizedName == normalizedName && b.Id != request.Id, cancellationToken))
            throw new AlreadyExistException(nameof(Branch), nameof(request.Name), request.Name);

        branch.Name = request.Name.Trim();
        branch.NormalizedName = normalizedName;
        branch.IsActive = request.IsActive;

        return await context.SaveAsync(cancellationToken) > 0;
    }
}
