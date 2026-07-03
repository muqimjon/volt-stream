namespace VoltStream.Application.Features.Currencies.Commands;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;

public record UpdateAllCurrenciesCommand(List<CurrencyCommand> Items) : IRequest<bool>;

public class UpdateAllCurrenciesCommandHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<UpdateAllCurrenciesCommand, bool>
{
    public async Task<bool> Handle(UpdateAllCurrenciesCommand request, CancellationToken cancellationToken)
    {
        var incoming = request.Items;
        var existing = await context.Currencies.ToListAsync(cancellationToken);

        var toDelete = existing
            .Where(db => incoming.All(dto => dto.Id != db.Id))
            .ToList();

        foreach (var currency in toDelete)
            currency.IsDeleted = true;

        foreach (var dto in incoming)
        {
            var entity = existing.FirstOrDefault(e => e.Id == dto.Id);

            if (entity is null)
            {
                var newEntity = mapper.Map<Currency>(dto);
                await context.Currencies.AddAsync(newEntity, cancellationToken);
            }
            else
                mapper.Map(dto, entity);
        }

        return await context.SaveAsync(cancellationToken) > 0;
    }
}
