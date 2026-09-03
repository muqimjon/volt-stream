namespace VoltStream.Application.Features.Categories.Queries;

using MapsterMapper;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Categories.DTOs;

public record CategoryFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<CategoryDto>>;

public class CategoryFilterQueryHandler(
    IAppDbContext context,
    ICurrentUser user,
    IPagingMetadataWriter writer,
    IMapper mapper) : IRequestHandler<CategoryFilterQuery, IReadOnlyCollection<CategoryDto>>
{
    public async Task<IReadOnlyCollection<CategoryDto>> Handle(CategoryFilterQuery request, CancellationToken cancellationToken)
    {
        user.Sanitize(request);

        return user.ApplyToCategories(mapper.Map<IReadOnlyCollection<CategoryDto>>(await context.Categories
            .ToPagedListAsync(request, writer, cancellationToken)));
    }
}
