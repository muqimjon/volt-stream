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
        if (!user.Has(Permissions.BranchesAll))
            return Own();

        return requested ?? await DefaultIdAsync(cancellationToken);
    }

    public async Task<long?> ResolveFilterAsync(long? requested, bool allowAll, CancellationToken cancellationToken = default)
    {
        if (!user.Has(Permissions.BranchesAll))
            return Own();

        if (requested is not null)
            return requested;

        return allowAll ? null : await DefaultIdAsync(cancellationToken);
    }

    public void EnsureAccess(long branchId)
    {
        if (!user.Has(Permissions.BranchesAll) && branchId != Own())
            throw new ForbiddenException("Boshqa filial ma'lumoti.");
    }

    public async Task<long> WarehouseOfAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.Branches
            .Where(b => b.Id == branchId)
            .Select(b => b.WarehouseId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException("Filialga ombor biriktirilmagan.");

    public async Task<long?> WarehouseFilterAsync(long? requested, CancellationToken cancellationToken = default)
        => await ResolveFilterAsync(requested, true, cancellationToken) is long branchId
            ? await WarehouseOfAsync(branchId, cancellationToken)
            : null;

    public async Task EnsureWarehouseAccessAsync(long warehouseId, CancellationToken cancellationToken = default)
    {
        if (!user.Has(Permissions.BranchesAll) && warehouseId != await WarehouseOfAsync(Own(), cancellationToken))
            throw new ForbiddenException("Boshqa filial ombori.");
    }

    private long Own()
        => user.BranchId ?? throw new ForbiddenException("Filial biriktirilmagan");

    private async Task<long> DefaultIdAsync(CancellationToken cancellationToken)
    {
        var id = await context.Branches
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return id > 0 ? id : throw new NotFoundException(nameof(Branch));
    }
}
