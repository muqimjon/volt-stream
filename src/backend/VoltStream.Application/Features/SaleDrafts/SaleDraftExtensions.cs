namespace VoltStream.Application.Features.SaleDrafts;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.SaleDrafts.DTOs;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

public record SaleDraftItemCommand(long ProductId, decimal LengthPerRoll, int RollCount, decimal TotalLength);

public static class SaleDraftExtensions
{
    public const string NotOpenMessage = "Ro'yxat allaqachon tasdiqlangan yoki bekor qilingan.";

    public static bool CanUseDrafts(this ICurrentUser user)
        => user.Has(Permissions.DraftsManage) || user.Has(Permissions.DraftsConfirm);

    public static bool SeesWholeBranch(this ICurrentUser user)
        => user.Has(Permissions.DraftsConfirm) || (user.Has(Permissions.DraftsManage) && user.Has(Permissions.ReportsView));

    public static bool CanAccess(this ICurrentUser user, SaleDraft draft, bool edit)
        => user.Has(Permissions.DraftsConfirm)
            || (user.Has(Permissions.DraftsManage) && (draft.CreatedByUserId == user.UserId || (!edit && user.Has(Permissions.ReportsView))));

    public static IQueryable<SaleDraft> WithDetails(this IQueryable<SaleDraft> drafts)
        => drafts
            .Include(d => d.Branch)
            .Include(d => d.Customer)
            .Include(d => d.Items.OrderBy(i => i.Id))
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Category);

    public static async Task<SaleDraft> FindDraftAsync(
        this IAppDbContext context,
        long id,
        ICurrentUser user,
        BranchContext branches,
        bool edit,
        CancellationToken cancellationToken)
    {
        var draft = await context.SaleDrafts.AsNoTracking().WithDetails().FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(SaleDraft), nameof(SaleDraft.Id), id);

        branches.EnsureAccess(draft.BranchId);

        return user.CanAccess(draft, edit) ? draft : throw new ForbiddenException("Boshqa xodim ro'yxati.");
    }

    // Shartli UPDATE qatorni tranzaksiya oxirigacha qulflaydi: parallel tahrir, bekor qilish va tasdiq
    // bir-birini kutadi va Open holati qayta tekshiriladi.
    public static async Task LockOpenDraftAsync(this IAppDbContext context, long id, CancellationToken cancellationToken)
    {
        var locked = await context.SaleDrafts
            .Where(d => d.Id == id && d.Status == SaleDraftStatus.Open)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);

        if (locked == 0)
            throw await context.SaleDrafts.AnyAsync(d => d.Id == id, cancellationToken)
                ? new ConflictException(NotOpenMessage)
                : new NotFoundException(nameof(SaleDraft), nameof(SaleDraft.Id), id);
    }

    public static async Task FillAsync(
        this SaleDraft draft,
        long? customerId,
        IReadOnlyCollection<SaleDraftItemCommand> items,
        IAppDbContext context,
        CancellationToken cancellationToken)
    {
        if (items is not { Count: > 0 })
            throw new AppException("Ro'yxatda mahsulot yo'q.");

        if (items.Any(i => !RollComposition.IsConsistent(i.RollCount, i.LengthPerRoll, i.TotalLength)))
            throw new AppException("Ro'yxat qatori noto'g'ri: soni va uzunligi musbat, jami (soni − 1) × uzunlikdan katta va soni × uzunlikdan oshmasin.");

        var productIds = items.Select(i => i.ProductId).Distinct().ToList();

        if (await context.Products.CountAsync(p => productIds.Contains(p.Id) && !p.IsDeleted, cancellationToken) != productIds.Count)
            throw new NotFoundException(nameof(Product));

        if (customerId is long id && !await context.Customers.AnyAsync(c => c.Id == id && !c.IsDeleted, cancellationToken))
            throw new NotFoundException(nameof(Customer), nameof(Customer.Id), id);

        draft.CustomerId = customerId;
        draft.Items.Clear();

        foreach (var item in items)
            draft.Items.Add(new SaleDraftItem
            {
                ProductId = item.ProductId,
                LengthPerRoll = item.LengthPerRoll,
                RollCount = item.RollCount,
                TotalLength = item.TotalLength
            });
    }

    public static async Task<IReadOnlyList<SaleDraftDto>> ToDtosAsync(
        this IReadOnlyCollection<SaleDraft> drafts,
        IAppDbContext context,
        CancellationToken cancellationToken)
    {
        var userIds = drafts.Select(d => d.CreatedByUserId).OfType<long>().Distinct().ToList();

        var usernames = await context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

        return [.. drafts.Select(draft => new SaleDraftDto
        {
            Id = draft.Id,
            Date = draft.Date,
            Description = draft.Description,
            Status = draft.Status,
            BranchId = draft.BranchId,
            Branch = draft.Branch?.Name ?? string.Empty,
            CustomerId = draft.CustomerId,
            Customer = draft.Customer?.Name,
            ConfirmedSaleId = draft.ConfirmedSaleId,
            CreatedByUserId = draft.CreatedByUserId,
            CreatedBy = draft.CreatedByUserId is long userId ? usernames.GetValueOrDefault(userId) : null,
            CreatedAt = draft.CreatedAt,
            TotalRolls = draft.Items.Sum(i => i.RollCount),
            TotalLength = draft.Items.Sum(i => i.TotalLength),
            Items = [.. draft.Items.Select(item => new SaleDraftItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                Product = item.Product?.Name ?? string.Empty,
                CategoryId = item.Product?.CategoryId ?? 0,
                Category = item.Product?.Category?.Name ?? string.Empty,
                Unit = item.Product?.Unit ?? string.Empty,
                LengthPerRoll = item.LengthPerRoll,
                RollCount = item.RollCount,
                TotalLength = item.TotalLength,
                Composition = RollComposition.Format(RollComposition.Of(item.RollCount, item.LengthPerRoll, item.TotalLength))
            })]
        })];
    }
}
