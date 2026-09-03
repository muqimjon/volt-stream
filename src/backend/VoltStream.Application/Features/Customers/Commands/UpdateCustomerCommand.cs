namespace VoltStream.Application.Features.Customers.Commands;

using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.CustomersManage)]
public record UpdateCustomerCommand(
    long Id,
    string Name,
    string? Phone,
    string? Address,
    string? Email,
    string? ImageUrl,
    string? ClientType,
    string? Description)
    : IRequest<bool>;

public class UpdateCustomerCommandHandler(
    IAppDbContext context,
    IMapper mapper) : IRequestHandler<UpdateCustomerCommand, bool>
{
    public async Task<bool> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customerExists = await context.Customers
            .AnyAsync(x => x.Id != request.Id && x.NormalizedName == request.Name.ToNormalized(), cancellationToken);

        if (customerExists)
            throw new AlreadyExistException(nameof(Customer));

        var customer = await context.Customers
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), nameof(request.Id), request.Id);

        mapper.Map(request, customer);

        return await context.SaveAsync(cancellationToken) > 0;
    }
}
