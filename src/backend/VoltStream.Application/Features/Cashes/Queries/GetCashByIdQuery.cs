namespace VoltStream.Application.Features.Cashes.Queries;

using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Cashes.DTOs;
using VoltStream.Domain.Entities;
using VoltStream.Application.Commons.Security;

[RequirePermission(Permissions.ReportsView)]
public record GetCashByIdQuery(long Id) : IRequest<CashDto>;


public class GetCashByIdQueryHandler(
    IAppDbContext context,
    IMapper mapper) : IRequestHandler<GetCashByIdQuery, CashDto>
{
    public async Task<CashDto> Handle(GetCashByIdQuery request, CancellationToken cancellationToken)
    {
        var cash = await context.Cashes
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Cash), nameof(request.Id), request.Id);

        return mapper.Map<CashDto>(cash);
    }
}