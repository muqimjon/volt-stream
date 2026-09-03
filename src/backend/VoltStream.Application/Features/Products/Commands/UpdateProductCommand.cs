namespace VoltStream.Application.Features.Products.Commands;

using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsProducts)]
public record UpdateProductCommand(
    long Id,
    string Unit,
    string Name,
    long CategoryId,
    bool IsWeightTracked = false,
    decimal? WeightPerUnit = null)
    : IRequest<bool>;

public class UpdateProductCommandHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<UpdateProductCommand, bool>
{
    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), nameof(request.Id), request.Id);

        var normalizedName = request.Name.ToNormalized();
        var productExists = await context.Products
            .AnyAsync(p => p.NormalizedName == normalizedName
                           && p.CategoryId == request.CategoryId
                           && p.Id != request.Id, cancellationToken);

        if (productExists)
            throw new AlreadyExistException(nameof(Product), "Name", request.Name);

        mapper.Map(request, product);
        product.NormalizedName = normalizedName;

        return await context.SaveAsync(cancellationToken) > 0;
    }
}