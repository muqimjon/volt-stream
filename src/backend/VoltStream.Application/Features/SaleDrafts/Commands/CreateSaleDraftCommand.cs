namespace VoltStream.Application.Features.SaleDrafts.Commands;

using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

[RequirePermission(Permissions.DraftsManage)]
public record CreateSaleDraftCommand(
    DateTimeOffset Date,
    long? CustomerId,
    string Description,
    List<SaleDraftItemCommand> Items,
    long? BranchId = null)
    : IRequest<long>;

public class CreateSaleDraftCommandHandler(IAppDbContext context, BranchContext branches)
    : IRequestHandler<CreateSaleDraftCommand, long>
{
    public async Task<long> Handle(CreateSaleDraftCommand request, CancellationToken cancellationToken)
    {
        var draft = new SaleDraft
        {
            Date = request.Date,
            Description = request.Description?.Trim() ?? string.Empty,
            Status = SaleDraftStatus.Open,
            BranchId = await branches.ResolveAsync(request.BranchId, cancellationToken)
        };

        await draft.FillAsync(request.CustomerId, request.Items, context, cancellationToken);

        context.SaleDrafts.Add(draft);
        await context.SaveAsync(cancellationToken);

        return draft.Id;
    }
}
