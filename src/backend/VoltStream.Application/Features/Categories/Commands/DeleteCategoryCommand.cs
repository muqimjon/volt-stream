namespace VoltStream.Application.Features.Categories.Commands;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsCategories)]
public record DeleteCategoryCommand(long Id) : IRequest<bool>;

public class DeleteCategoryCommandHandler(IAppDbContext context)
    : IRequestHandler<DeleteCategoryCommand, bool>
{
    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), nameof(request.Id), request.Id);

        var hasSoldProducts = await context.Products
            .Where(p => p.CategoryId == request.Id)
            .AnyAsync(p => context.SaleItems.Any(si => si.ProductId == p.Id), cancellationToken);

        if (hasSoldProducts)
            throw new ForbiddenException("Kategoriyani o'chirib bo'lmaydi: Undagi ayrim mahsulotlar savdoda ishtirok etgan.");

        context.Categories.Remove(category);
        await context.SaveAsync(cancellationToken);

        return true;
    }
}