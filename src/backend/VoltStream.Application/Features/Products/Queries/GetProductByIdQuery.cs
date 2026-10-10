namespace VoltStream.Application.Features.Products.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Features.Products.DTOs;
using VoltStream.Domain.Entities;

public record GetProductByIdQuery(long Id) : IRequest<ProductDto>;

public class GetProductByIdQueryHandler(
    IAppDbContext context,
    IMapper mapper) : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), nameof(request.Id), request.Id);

        return mapper.Map<ProductDto>(product);
    }
}