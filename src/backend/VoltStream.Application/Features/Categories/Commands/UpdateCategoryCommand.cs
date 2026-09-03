namespace VoltStream.Application.Features.Categories.Commands;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsCategories)]
public record UpdateCategoryCommand(
    long Id,
    string Name)
    : IRequest<bool>;

public class UpdateCategoryCommandHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<UpdateCategoryCommand, bool>
{
    public async Task<bool> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await context.Categories.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), nameof(request.Id), request.Id);

        var categoryExists = await context.Categories
            .AnyAsync(p => p.NormalizedName == request.Name.ToNormalized() && p.Id != request.Id, cancellationToken);

        if (categoryExists)
            throw new AlreadyExistException(nameof(Category), "Name", request.Name);

        mapper.Map(request, category);

        return await context.SaveAsync(cancellationToken) > 0;
    }
}