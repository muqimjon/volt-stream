namespace VoltStream.Application.Features.PrintJobs.Commands;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.PrintJobs.DTOs;
using VoltStream.Application.Features.SaleDrafts;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

// Ruxsat manbaga bog'liq (savdo — print.request, ro'yxat — drafts.print), shuning uchun atribut emas, handler tekshiradi.
public record CreatePrintJobCommand(
    long? SaleId = null,
    int Copies = 1,
    bool Force = false,
    long? BranchId = null,
    long? SaleDraftId = null)
    : IRequest<PrintJobDto>;

public class CreatePrintJobCommandHandler(
    IAppDbContext context,
    ICurrentUser user,
    BranchContext branches,
    IPrintJobNotifier notifier) : IRequestHandler<CreatePrintJobCommand, PrintJobDto>
{
    public async Task<PrintJobDto> Handle(CreatePrintJobCommand request, CancellationToken cancellationToken)
    {
        if ((request.SaleId is null) == (request.SaleDraftId is null))
            throw new AppException("Savdo yoki ro'yxatdan aynan bittasi ko'rsatilishi kerak.");

        var permission = request.SaleDraftId is null ? Permissions.PrintRequest : Permissions.DraftsPrint;

        if (!user.Has(permission))
            throw new ForbiddenException($"Ruxsat yetarli emas: {permission}");

        var requester = await context.Users.FirstOrDefaultAsync(u => u.Id == user.UserId, cancellationToken)
            ?? throw new ForbiddenException("Foydalanuvchi aniqlanmadi.");

        if (requester.PrintPolicy == PrintPolicy.Deny)
            throw new ForbiddenException("Chop etish taqiqlangan.");

        var sourceBranchId = request.SaleDraftId is long draftId
            ? await DraftBranchAsync(draftId, request.Force, cancellationToken)
            : await SaleBranchAsync(request.SaleId!.Value, request.Force, cancellationToken);

        if (!request.Force && await context.PrintJobs.AnyAsync(job =>
                job.SaleId == request.SaleId && job.SaleDraftId == request.SaleDraftId
                && (job.Status == PrintJobStatus.Pending || job.Status == PrintJobStatus.Printing), cancellationToken))
            throw new ConflictException(request.SaleId != null
                ? "Bu savdo uchun faol chop etish so'rovi mavjud."
                : "Bu ro'yxat uchun faol chop etish so'rovi mavjud.");

        var branchId = user.Has(Permissions.BranchesAll) ? request.BranchId ?? sourceBranchId : sourceBranchId;

        if (branchId != sourceBranchId && !await context.Branches.AnyAsync(branch => branch.Id == branchId, cancellationToken))
            throw new NotFoundException(nameof(Branch), nameof(request.BranchId), branchId);

        var job = new PrintJob
        {
            SaleId = request.SaleId,
            SaleDraftId = request.SaleDraftId,
            BranchId = branchId,
            RequestedByUserId = requester.Id,
            Status = PrintJobStatus.Pending,
            Copies = Math.Clamp(request.Copies, 1, 5)
        };

        context.PrintJobs.Add(job);
        await context.SaveAsync(cancellationToken);

        var dto = await job.ToDtoAsync(context, cancellationToken);
        await notifier.JobCreated(dto);

        return dto;
    }

    private async Task<long> SaleBranchAsync(long saleId, bool force, CancellationToken cancellationToken)
    {
        var sale = await context.Sales.FirstOrDefaultAsync(s => s.Id == saleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Sale), nameof(CreatePrintJobCommand.SaleId), saleId);

        if (!user.Has(Permissions.BranchesAll) && sale.BranchId != user.BranchId)
            throw new ForbiddenException("Boshqa filial savdosini chop etib bo'lmaydi.");

        if (force && !user.Has(Permissions.SalesEdit))
            throw new ForbiddenException("Qayta chop etishga ruxsat yo'q.");

        return sale.BranchId;
    }

    private async Task<long> DraftBranchAsync(long draftId, bool force, CancellationToken cancellationToken)
    {
        var draft = await context.SaleDrafts.AsNoTracking().FirstOrDefaultAsync(d => d.Id == draftId, cancellationToken)
            ?? throw new NotFoundException(nameof(SaleDraft), nameof(CreatePrintJobCommand.SaleDraftId), draftId);

        branches.EnsureAccess(draft.BranchId);

        if (!user.CanAccess(draft, edit: false))
            throw new ForbiddenException("Boshqa xodim ro'yxati.");

        if (draft.Status != SaleDraftStatus.Open)
            throw new ConflictException(SaleDraftExtensions.NotOpenMessage);

        if (force && !user.Has(Permissions.DraftsConfirm))
            throw new ForbiddenException("Qayta chop etishga ruxsat yo'q.");

        return draft.BranchId;
    }
}
