namespace VoltStream.Application.Features.Customers.Commands;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.CustomersManage)]
public record DeleteCustomerCommand(long Id) : IRequest<bool>;

public class DeleteCustomerCommandHandler(
    IAppDbContext context)
    : IRequestHandler<DeleteCustomerCommand, bool>
{
    public async Task<bool> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), nameof(request.Id), request.Id);

        var hasOperations = await context.CustomerOperations
            .AnyAsync(co => co.CustomerId == request.Id, cancellationToken);

        if (hasOperations)
            throw new ForbiddenException("Mijozni o'chirib bo'lmaydi: Unda savdo yoki to'lov operatsiyalari mavjud.");

        if (await context.SaleDrafts.AnyAsync(d => d.CustomerId == request.Id, cancellationToken))
            throw new ConflictException("Mijozni o'chirib bo'lmaydi: U yuk ro'yxatlarida ko'rsatilgan.");

        context.Customers.Remove(customer);
        await context.SaveAsync(cancellationToken);

        return true;
    }
}
