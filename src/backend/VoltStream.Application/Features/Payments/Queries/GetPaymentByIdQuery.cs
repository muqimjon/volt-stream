namespace VoltStream.Application.Features.Payments.Queries;

using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Payments.DTOs;
using VoltStream.Domain.Entities;
using VoltStream.Application.Commons.Security;

[RequirePermission(Permissions.ReportsView)]
public record GetPaymentByIdQuery(long Id) : IRequest<PaymentDto>;

public class GetPaymentByIdQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetPaymentByIdQuery, PaymentDto>
{
    public async Task<PaymentDto> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), nameof(request.Id), request.Id);

        return mapper.Map<PaymentDto>(payment);
    }
}