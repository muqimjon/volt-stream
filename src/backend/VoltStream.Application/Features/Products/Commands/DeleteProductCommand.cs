namespace VoltStream.Application.Features.Products.Commands;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsProducts)]
public record DeleteProductCommand(long Id) : IRequest<bool>;

public class DeleteProductCommandHandler(IAppDbContext context)
    : IRequestHandler<DeleteProductCommand, bool>
{
    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), nameof(request.Id), request.Id);

        var isSold = await context.SaleItems
            .AnyAsync(si => si.ProductId == request.Id, cancellationToken);

        if (isSold)
            throw new ForbiddenException("Ushbu mahsulotni o'chirib bo'lmaydi: U bilan bog'liq savdo operatsiyalari mavjud.");

        context.Products.Remove(product);
        await context.SaveAsync(cancellationToken);

        return true;
    }
}