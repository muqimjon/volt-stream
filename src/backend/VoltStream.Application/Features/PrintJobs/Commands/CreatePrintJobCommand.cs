namespace VoltStream.Application.Features.PrintJobs.Commands;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.PrintJobs.DTOs;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

[RequirePermission(Permissions.PrintRequest)]
public record CreatePrintJobCommand(long SaleId, int Copies = 1, bool Force = false) : IRequest<PrintJobDto>;

public class CreatePrintJobCommandHandler(
    IAppDbContext context,
    ICurrentUser user,
    IPrintJobNotifier notifier) : IRequestHandler<CreatePrintJobCommand, PrintJobDto>
{
    public async Task<PrintJobDto> Handle(CreatePrintJobCommand request, CancellationToken cancellationToken)
    {
        var requester = await context.Users.FirstOrDefaultAsync(u => u.Id == user.UserId, cancellationToken)
            ?? throw new ForbiddenException("Foydalanuvchi aniqlanmadi.");

        if (requester.PrintPolicy == PrintPolicy.Deny)
            throw new ForbiddenException("Chop etish taqiqlangan.");

        var sale = await context.Sales.FirstOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Sale), nameof(request.SaleId), request.SaleId);

        if (!user.Has(Permissions.BranchesAll) && sale.BranchId != user.BranchId)
            throw new ForbiddenException("Boshqa filial savdosini chop etib bo'lmaydi.");

        if (!request.Force && await context.PrintJobs.AnyAsync(job => job.SaleId == sale.Id
                && (job.Status == PrintJobStatus.Pending || job.Status == PrintJobStatus.Printing), cancellationToken))
            throw new ConflictException("Bu savdo uchun faol chop etish so'rovi mavjud.");

        var job = new PrintJob
        {
            SaleId = sale.Id,
            BranchId = sale.BranchId,
            RequestedByUserId = requester.Id,
            Status = PrintJobStatus.Pending,
            Copies = request.Copies < 1 ? 1 : request.Copies
        };

        context.PrintJobs.Add(job);
        await context.SaveAsync(cancellationToken);

        var dto = await job.ToDtoAsync(context, cancellationToken);
        await notifier.JobCreated(dto);

        return dto;
    }
}
