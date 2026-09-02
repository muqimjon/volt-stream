namespace VoltStream.Application.Commons.Services;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

public class BranchContext(IAppDbContext context, ICurrentUser user)
{
    public async Task<long> ResolveAsync(long? requested, CancellationToken cancellationToken = default)
    {
        if (!user.Has(Permissions.BranchesAll) && user.BranchId is long own)
            return own;

        return requested ?? await DefaultIdAsync(cancellationToken);
    }

    public async Task<long?> ResolveFilterAsync(long? requested, bool allowAll, CancellationToken cancellationToken = default)
    {
        if (!user.Has(Permissions.BranchesAll) && user.BranchId is long own)
            return own;

        if (requested is not null)
            return requested;

        return allowAll ? null : await DefaultIdAsync(cancellationToken);
    }

    private async Task<long> DefaultIdAsync(CancellationToken cancellationToken)
    {
        var id = await context.Branches
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return id > 0 ? id : throw new NotFoundException(nameof(Branch));
    }
}
