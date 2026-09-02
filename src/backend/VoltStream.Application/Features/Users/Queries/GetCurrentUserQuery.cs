namespace VoltStream.Application.Features.Users.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Users.DTOs;
using VoltStream.Domain.Entities;

public record GetCurrentUserQuery : IRequest<UserDto>;

public class GetCurrentUserQueryHandler(
    IAppDbContext context,
    ICurrentUser currentUser,
    IMapper mapper)
    : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        => mapper.Map<UserDto>(await context.Users
            .AsNoTracking()
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken))
        ?? throw new NotFoundException(nameof(User));
}
