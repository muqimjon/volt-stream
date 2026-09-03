namespace VoltStream.Application.Features.Products.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Domain.Entities;

public record GetAllProductsByCategoryIdQuery(long CategoryId) : IRequest<IReadOnlyCollection<Product>>;

public class GetAllProductsByCategoryIdQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetAllProductsByCategoryIdQuery, IReadOnlyCollection<Product>>
{
    public async Task<IReadOnlyCollection<Product>> Handle(GetAllProductsByCategoryIdQuery request, CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyCollection<Product>>
        (await context.Products.Where(a => a.CategoryId == request.CategoryId).ToListAsync(cancellationToken));
}
