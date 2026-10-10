namespace VoltStream.Application.Tests.Drafts;

using VoltStream.Application.Features.SaleDrafts.Commands;
using VoltStream.Application.Features.Stats.DTOs;
using VoltStream.Application.Features.Stats.Queries;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Domain.Enums;

[Collection(DatabaseCollection.Name)]
public sealed class SaleDraftStatsTests(PostgresFixture db)
{
    private static readonly DateTimeOffset From = DateTimeOffset.UtcNow.AddDays(-1);
    private static readonly DateTimeOffset To = DateTimeOffset.UtcNow.AddDays(1);

    private static async Task<(bool Own, int Count, int Open, int Confirmed, int Cancelled, int Rolls, decimal Length)> Drafts(
        Actor actor, long? branchId = null, long? userId = null)
    {
        BranchStatsDto stats = await actor.Send(new GetBranchStatsQuery(From, To, branchId, userId));

        return (stats.IsOwnScope, stats.DraftsCount, stats.DraftsOpen, stats.DraftsConfirmed, stats.DraftsCancelled, stats.DraftRolls, stats.DraftLength);
    }

    [Fact]
    public async Task Draft_numbers_follow_the_own_or_branch_scope()
    {
        var w = await db.NewWorld();
        var product = await w.NewProduct();
        await w.Supply(product, 20, 100, 5_000);
        var customer = await w.NewCustomer();
        var colleague = w.As(TestUser.Of(UserRole.Seller, w.Branch));

        var confirmed = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        var cancelled = await w.Seller.Send(w.Draft([new Line(product, 1, 50)]));
        await w.Seller.Send(w.Draft([new Line(product, 1, 100, Length: 60)]));
        await colleague.Send(w.Draft([new Line(product, 3, 100)]));
        await w.OtherSeller.Send(w.Draft([new Line(product, 4, 100)], branchId: w.OtherBranch));

        await w.Manager.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]) with { SaleDraftId = confirmed });
        await w.Seller.Send(new CancelSaleDraftCommand(cancelled));

        var sellers = (true, 3, 1, 1, 1, 4, 310m);
        Assert.Equal(sellers, await Drafts(w.Seller));
        Assert.Equal(sellers, await Drafts(w.Seller, userId: colleague.User.UserId));
        Assert.Equal((true, 1, 1, 0, 0, 3, 300m), await Drafts(colleague));
        Assert.Equal((true, 1, 1, 0, 0, 4, 400m), await Drafts(w.OtherSeller));

        Assert.Equal((false, 4, 2, 1, 1, 7, 610m), await Drafts(w.Manager));
        Assert.Equal(sellers, await Drafts(w.Manager, userId: w.Seller.User.UserId));
        Assert.Equal((false, 1, 1, 0, 0, 4, 400m), await Drafts(w.Admin, w.OtherBranch));
    }
}
