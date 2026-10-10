namespace VoltStream.Application.Features.Products.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Products.DTOs;

public record ProductFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<ProductDto>>;

public class ProductFilterQueryHandler(
    IAppDbContext context,
    ICurrentUser user,
    IPagingMetadataWriter writer,
    IMapper mapper) : IRequestHandler<ProductFilterQuery, IReadOnlyCollection<ProductDto>>
{
    public async Task<IReadOnlyCollection<ProductDto>> Handle(ProductFilterQuery request, CancellationToken cancellationToken)
    {
        user.Sanitize(request);

        var query = context.Products.AsQueryable();

        if (!user.Has(Permissions.SalesViewPrices))
            query = query.Include(product => product.Category);

        return user.ApplyToProducts(mapper.Map<IReadOnlyCollection<ProductDto>>(await query
            .ToPagedListAsync(request, writer, cancellationToken)));
    }
}
