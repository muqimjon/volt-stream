namespace VoltStream.Application.Features.Payments.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Payments.DTOs;

public record GetAllPaymentsQuery : IRequest<IReadOnlyCollection<PaymentDto>>;

public class GetAllPaymentsQueryHandler(
    IAppDbContext context,
    IMapper mapper) : IRequestHandler<GetAllPaymentsQuery, IReadOnlyCollection<PaymentDto>>
{
    public async Task<IReadOnlyCollection<PaymentDto>> Handle(GetAllPaymentsQuery request, CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyCollection<PaymentDto>>(await context.Payments
            .AsNoTracking()
            .Include(p => p.CustomerOperation)
            .ToListAsync(cancellationToken));
}
