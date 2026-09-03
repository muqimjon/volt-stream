namespace VoltStream.Application.Features.Categories.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Features.Categories.DTOs;

public record GetAllCategoriesQuery : IRequest<IReadOnlyCollection<CategoryDto>>;

public class GetAllCategoriesQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetAllCategoriesQuery, IReadOnlyCollection<CategoryDto>>
{
    public async Task<IReadOnlyCollection<CategoryDto>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyCollection<CategoryDto>>(await context.Categories.ToListAsync(cancellationToken));
}