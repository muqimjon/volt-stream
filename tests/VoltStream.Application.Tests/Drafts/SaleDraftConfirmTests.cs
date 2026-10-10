namespace VoltStream.Application.Tests.Drafts;

using System.Net;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Sales.Commands;
using VoltStream.Application.Features.SaleDrafts.Commands;
using VoltStream.Application.Features.SaleDrafts.Queries;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Application.Tests.Ledger;
using VoltStream.Domain.Enums;

[Collection(DatabaseCollection.Name)]
public sealed class SaleDraftConfirmTests(PostgresFixture db)
{
    private async Task<(World World, long Product, long Customer, long Draft)> Arrange()
    {
        var world = await db.NewWorld();
        var product = await world.NewProduct();
        await world.Supply(product, 10, 100, 5_000);
        var customer = await world.NewCustomer();

        return (world, product, customer, await world.Seller.Send(world.Draft([new Line(product, 2, 100)], customer)));
    }

    private static CreateSaleCommand Confirmation(World w, long draft, long customer, long product, int rolls = 2)
        => w.Sale(customer, w.Uzs, [new Line(product, rolls, 100, 5_000)]) with { SaleDraftId = draft };

    private static async Task<SaleDraftStatus> StatusOf(World w, long draft)
        => (await w.Manager.Send(new GetSaleDraftByIdQuery(draft))).Status;

    [Fact]
    public async Task Confirming_makes_exactly_one_sale_and_closes_the_list()
    {
        var (w, product, customer, draft) = await Arrange();

        var saleId = await w.Manager.Send(Confirmation(w, draft, customer, product));

        var confirmed = await w.Manager.Send(new GetSaleDraftByIdQuery(draft));
        Assert.Equal(SaleDraftStatus.Confirmed, confirmed.Status);
        Assert.Equal(saleId, confirmed.ConfirmedSaleId);
        Assert.DoesNotContain(draft, await w.OpenDraftIds(w.Manager));
        Assert.DoesNotContain(draft, await w.OpenDraftIds(w.Seller));
        Assert.Equal([saleId], (await w.SalesOf(customer)).Select(sale => sale.Id));
        Assert.Equal([new StockRow(100, 8, 800)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_000_000));
    }

    [Fact]
    public async Task Confirming_needs_drafts_confirm_and_sales_create()
    {
        var (w, product, customer, draft) = await Arrange();
        var withoutSales = w.As(TestUser.With(w.Branch, Permissions.DraftsConfirm));
        var withoutConfirm = w.As(TestUser.With(w.Branch, Permissions.SalesCreate));

        foreach (var actor in new[] { w.Seller, w.Cashier, withoutSales, withoutConfirm })
            await Rejection.With(HttpStatusCode.Forbidden, () => actor.Send(Confirmation(w, draft, customer, product)));

        Assert.Equal(SaleDraftStatus.Open, await StatusOf(w, draft));
        Assert.Empty(await w.SalesOf(customer));
        Assert.Equal([new StockRow(100, 10, 1_000)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0));
    }

    [Fact]
    public async Task A_second_confirmation_is_a_conflict()
    {
        var (w, product, customer, draft) = await Arrange();
        await w.Manager.Send(Confirmation(w, draft, customer, product));

        await Rejection.With(HttpStatusCode.Conflict, () => w.Manager.Send(Confirmation(w, draft, customer, product)));

        Assert.Single(await w.SalesOf(customer));
        Assert.Equal([new StockRow(100, 8, 800)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_000_000));
    }

    [Fact]
    public async Task Racing_confirmations_make_only_one_sale()
    {
        var (w, product, customer, draft) = await Arrange();

        var outcomes = await Racing.Race(() => w.Manager.Send(Confirmation(w, draft, customer, product)), 8);

        Assert.Equal(1, outcomes.Won());
        Assert.All(outcomes.Lost(), outcome => Assert.IsType<ConflictException>(outcome.Error));
        Assert.Single(await w.SalesOf(customer));
        Assert.Equal([new StockRow(100, 8, 800)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_000_000));
    }

    [Fact]
    public async Task A_cancel_racing_a_confirmation_leaves_one_consistent_outcome()
    {
        var (w, product, customer, _) = await Arrange();

        for (var round = 0; round < 6; round++)
        {
            var draft = await w.Seller.Send(w.Draft([new Line(product, 1, 100)], customer));
            var sales = (await w.SalesOf(customer)).Count;

            var outcomes = await Racing.Race([
                () => w.Manager.Send(Confirmation(w, draft, customer, product, rolls: 1)),
                () => w.Manager.Send(new CancelSaleDraftCommand(draft))]);

            Assert.Equal(1, outcomes.Won());
            Assert.All(outcomes.Lost(), outcome => Assert.IsType<ConflictException>(outcome.Error));
            var status = await StatusOf(w, draft);
            Assert.Equal(status == SaleDraftStatus.Confirmed ? sales + 1 : sales, (await w.SalesOf(customer)).Count);
        }

        var confirmed = (await w.SalesOf(customer)).Count;
        await LedgerAssert.Balances(w, customer, (w.Uzs, -confirmed * 500_000));
    }

    [Fact]
    public async Task A_list_of_another_branch_cannot_be_confirmed()
    {
        var (w, product, customer, draft) = await Arrange();
        await w.Supply(product, 10, 100, 5_000, branchId: w.OtherBranch);

        await Rejection.With(HttpStatusCode.NotFound, () => w.OtherManager.Send(Confirmation(w, draft, customer, product)));
        await Rejection.With(HttpStatusCode.Conflict, () => w.Admin.Send(Confirmation(w, draft, customer, product) with { BranchId = w.OtherBranch }));

        Assert.Equal(SaleDraftStatus.Open, await StatusOf(w, draft));
        Assert.Empty(await w.SalesOf(customer));
        Assert.Equal([new StockRow(100, 10, 1_000)], await w.Stock(product, w.OtherBranch));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0));
    }

    [Fact]
    public async Task A_missing_list_is_not_found()
    {
        var (w, product, customer, _) = await Arrange();

        await Rejection.With(HttpStatusCode.NotFound, () => w.Manager.Send(Confirmation(w, 999_999_999, customer, product)));

        Assert.Empty(await w.SalesOf(customer));
        Assert.Equal([new StockRow(100, 10, 1_000)], await w.Stock(product));
    }

    [Fact]
    public async Task A_sale_that_fails_after_the_claim_leaves_the_list_open()
    {
        var (w, product, customer, draft) = await Arrange();
        var neverSupplied = w.Sale(customer, w.Uzs, [new Line(product, 1, 70, 5_000)]) with { SaleDraftId = draft };

        await Rejection.With(HttpStatusCode.NotFound, () => w.Manager.Send(neverSupplied));

        Assert.Equal(SaleDraftStatus.Open, await StatusOf(w, draft));
        Assert.Empty(await w.SalesOf(customer));
        await w.Manager.Send(Confirmation(w, draft, customer, product));
        Assert.Equal(SaleDraftStatus.Confirmed, await StatusOf(w, draft));
    }

    [Fact]
    public async Task A_sale_that_does_not_fit_the_strict_stock_leaves_the_list_open()
    {
        await using var database = await db.NewDatabase();
        var w = await database.NewWorld();
        var product = await w.NewProduct();
        await w.Supply(product, 1, 100, 5_000);
        await w.SetNegativeStock(false);
        var customer = await w.NewCustomer();
        var draft = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));

        await Rejection.With(HttpStatusCode.Conflict, () => w.Manager.Send(Confirmation(w, draft, customer, product)));

        Assert.Equal(SaleDraftStatus.Open, await StatusOf(w, draft));
        Assert.Empty(await w.SalesOf(customer));
        Assert.Equal([new StockRow(100, 1, 100)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0));

        await w.Supply(product, 5, 100, 5_000);
        await w.Manager.Send(Confirmation(w, draft, customer, product));
        Assert.Equal(SaleDraftStatus.Confirmed, await StatusOf(w, draft));
    }

    [Fact]
    public async Task Editing_the_confirmed_sale_moves_the_link_to_the_new_sale()
    {
        var (w, product, customer, draft) = await Arrange();
        var saleId = await w.Manager.Send(Confirmation(w, draft, customer, product));

        await w.Manager.Send(World.Edit(saleId, w.Sale(customer, w.Uzs, [new Line(product, 3, 100, 5_000)])));

        var newSale = (await w.LastSale(customer)).Id;
        var confirmed = await w.Manager.Send(new GetSaleDraftByIdQuery(draft));
        Assert.NotEqual(saleId, newSale);
        Assert.Equal(newSale, confirmed.ConfirmedSaleId);
        Assert.Equal(SaleDraftStatus.Confirmed, confirmed.Status);
        Assert.Single(await w.SalesOf(customer));
        Assert.Equal([new StockRow(100, 7, 700)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_500_000));
    }

    [Fact]
    public async Task Editing_an_ordinary_sale_never_attaches_a_list()
    {
        var (w, product, customer, draft) = await Arrange();
        var ordinary = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 5_000)]));

        await w.Manager.Send(World.Edit(ordinary, w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)])));

        var open = await w.Manager.Send(new GetSaleDraftByIdQuery(draft));
        Assert.Equal(SaleDraftStatus.Open, open.Status);
        Assert.Null(open.ConfirmedSaleId);
        Assert.Null(typeof(UpdateSaleCommand).GetProperty(nameof(CreateSaleCommand.SaleDraftId)));
    }

    [Fact]
    public async Task Deleting_the_confirmed_sale_reopens_the_list_for_a_new_confirmation()
    {
        var (w, product, customer, draft) = await Arrange();
        var saleId = await w.Manager.Send(Confirmation(w, draft, customer, product));

        await w.DeleteSale(saleId);

        var reopened = await w.Manager.Send(new GetSaleDraftByIdQuery(draft));
        Assert.Equal(SaleDraftStatus.Open, reopened.Status);
        Assert.Null(reopened.ConfirmedSaleId);
        Assert.Contains(draft, await w.OpenDraftIds(w.Manager));
        Assert.Equal([new StockRow(100, 10, 1_000)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0));

        var again = await w.Manager.Send(Confirmation(w, draft, customer, product));

        Assert.NotEqual(saleId, again);
        Assert.Equal(again, (await w.Manager.Send(new GetSaleDraftByIdQuery(draft))).ConfirmedSaleId);
        Assert.Equal(SaleDraftStatus.Confirmed, await StatusOf(w, draft));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_000_000));
    }
}
