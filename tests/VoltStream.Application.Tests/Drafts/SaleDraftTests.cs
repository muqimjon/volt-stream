namespace VoltStream.Application.Tests.Drafts;

using System.Net;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Categories.Commands;
using VoltStream.Application.Features.Customers.Commands;
using VoltStream.Application.Features.Products.Commands;
using VoltStream.Application.Features.SaleDrafts.Commands;
using VoltStream.Application.Features.SaleDrafts.Queries;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Application.Tests.Ledger;
using VoltStream.Domain.Enums;

[Collection(DatabaseCollection.Name)]
public sealed class SaleDraftTests(PostgresFixture db)
{
    private async Task<(World World, long Product, long Customer)> Arrange()
    {
        var world = await db.NewWorld();
        var product = await world.NewProduct();
        await world.Supply(product, 10, 100, 5_000);

        return (world, product, await world.NewCustomer((world.Uzs, 250_000)));
    }

    private static string[] DraftPermissions(UserRole role)
        => [.. RolePresets.For(role).Where(permission => permission.StartsWith("drafts.")).Order()];

    [Fact]
    public void Role_presets_grant_draft_permissions_as_agreed()
    {
        Assert.Equal([Permissions.DraftsManage], DraftPermissions(UserRole.Seller));
        Assert.Equal([Permissions.DraftsConfirm, Permissions.DraftsManage], DraftPermissions(UserRole.Manager));
        Assert.Empty(DraftPermissions(UserRole.Cashier));
        Assert.Equal([Permissions.DraftsConfirm, Permissions.DraftsManage, Permissions.DraftsPrint], DraftPermissions(UserRole.Admin));
    }

    [Fact]
    public async Task Creating_editing_and_cancelling_a_list_leaves_stock_and_balances_alone()
    {
        var (w, product, customer) = await Arrange();
        var stock = await w.Stock(product);

        async Task AssertUntouched()
        {
            Assert.Equal(stock, await w.Stock(product));
            Assert.Empty(await w.SalesOf(customer));
            await LedgerAssert.Balances(w, customer, (w.Uzs, 250_000));
        }

        var id = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        await AssertUntouched();

        await w.Seller.Send(World.EditDraft(id, w.Draft([new Line(product, 3, 100, Length: 250)], customer)));
        await AssertUntouched();
        var edited = Assert.Single((await w.Seller.Send(new GetSaleDraftByIdQuery(id))).Items);
        Assert.Equal("2 × 100 + 1 × 50", edited.Composition);

        await w.Seller.Send(new CancelSaleDraftCommand(id));
        await AssertUntouched();
        Assert.Equal(SaleDraftStatus.Cancelled, (await w.Seller.Send(new GetSaleDraftByIdQuery(id))).Status);
        Assert.DoesNotContain(id, await w.OpenDraftIds(w.Seller));
    }

    [Fact]
    public async Task A_seller_sees_and_edits_only_their_own_lists()
    {
        var (w, product, customer) = await Arrange();
        var colleague = w.As(TestUser.Of(UserRole.Seller, w.Branch));
        var mine = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        var theirs = await colleague.Send(w.Draft([new Line(product, 1, 100)], customer));

        Assert.Equal([mine], await w.OpenDraftIds(w.Seller));
        Assert.Equal([theirs], await w.OpenDraftIds(colleague));
        Assert.Equal(mine, (await w.Seller.Send(new GetSaleDraftByIdQuery(mine))).Id);

        await Rejection.With(HttpStatusCode.Forbidden, () => colleague.Send(new GetSaleDraftByIdQuery(mine)));
        await Rejection.With(HttpStatusCode.Forbidden, () => colleague.Send(World.EditDraft(mine, w.Draft([new Line(product, 5, 100)], customer))));
        await Rejection.With(HttpStatusCode.Forbidden, () => colleague.Send(new CancelSaleDraftCommand(mine)));
        Assert.Equal(2, Assert.Single((await w.Seller.Send(new GetSaleDraftByIdQuery(mine))).Items).RollCount);
    }

    [Fact]
    public async Task Report_permission_widens_viewing_but_not_editing()
    {
        var (w, product, customer) = await Arrange();
        var reader = w.As(TestUser.With(w.Branch, Permissions.DraftsManage, Permissions.ReportsView));
        var mine = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));

        Assert.Equal([mine], await w.OpenDraftIds(reader));
        Assert.Equal(mine, (await reader.Send(new GetSaleDraftByIdQuery(mine))).Id);
        await Rejection.With(HttpStatusCode.Forbidden, () => reader.Send(World.EditDraft(mine, w.Draft([new Line(product, 5, 100)], customer))));
        await Rejection.With(HttpStatusCode.Forbidden, () => reader.Send(new CancelSaleDraftCommand(mine)));
    }

    [Fact]
    public async Task Confirm_permission_sees_and_edits_the_whole_branch()
    {
        var (w, product, customer) = await Arrange();
        var first = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        var second = await w.As(TestUser.Of(UserRole.Seller, w.Branch)).Send(w.Draft([new Line(product, 1, 100)], customer));

        var confirmOnly = w.As(TestUser.With(w.Branch, Permissions.DraftsConfirm));

        Assert.Equal([first, second], await w.OpenDraftIds(w.Manager));
        Assert.Equal([first, second], await w.OpenDraftIds(confirmOnly));
        Assert.True(await confirmOnly.Send(World.EditDraft(first, w.Draft([new Line(product, 4, 100)], customer))));
        Assert.True(await w.Manager.Send(new CancelSaleDraftCommand(second)));
        Assert.Equal([first], await w.OpenDraftIds(confirmOnly));
    }

    [Fact]
    public async Task Cashiers_and_other_branches_are_kept_out()
    {
        var (w, product, customer) = await Arrange();
        var draft = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        var edit = World.EditDraft(draft, w.Draft([new Line(product, 5, 100)], customer));

        await Rejection.With(HttpStatusCode.Forbidden, () => w.Cashier.Send(w.Draft([new Line(product, 2, 100)], customer)));
        await Rejection.With(HttpStatusCode.Forbidden, () => w.Cashier.Send(new GetOpenSaleDraftsQuery()));
        foreach (var outsider in new[] { w.Cashier, w.OtherManager, w.OtherSeller })
        {
            await Rejection.With(HttpStatusCode.Forbidden, () => outsider.Send(new GetSaleDraftByIdQuery(draft)));
            await Rejection.With(HttpStatusCode.Forbidden, () => outsider.Send(edit));
            await Rejection.With(HttpStatusCode.Forbidden, () => outsider.Send(new CancelSaleDraftCommand(draft)));
        }

        Assert.DoesNotContain(draft, await w.OpenDraftIds(w.OtherManager));
        Assert.Equal(2, Assert.Single((await w.Seller.Send(new GetSaleDraftByIdQuery(draft))).Items).RollCount);
    }

    [Fact]
    public async Task A_non_admin_cannot_choose_the_branch()
    {
        var (w, product, customer) = await Arrange();

        var id = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer, branchId: w.OtherBranch));

        Assert.Equal(w.Branch, (await w.Seller.Send(new GetSaleDraftByIdQuery(id))).BranchId);
        Assert.Equal([id], await w.OpenDraftIds(w.Seller, w.OtherBranch));
        Assert.Empty(await w.OpenDraftIds(w.OtherManager));
    }

    [Fact]
    public async Task Admin_sees_every_branch_and_picks_one()
    {
        var (w, product, customer) = await Arrange();
        var own = await w.Admin.Send(w.Draft([new Line(product, 2, 100)], customer, branchId: w.Branch));
        var other = await w.Admin.Send(w.Draft([new Line(product, 1, 100)], customer, branchId: w.OtherBranch));

        Assert.Equal(w.OtherBranch, (await w.Admin.Send(new GetSaleDraftByIdQuery(other))).BranchId);
        Assert.Equal([own], await w.OpenDraftIds(w.Admin, w.Branch));
        Assert.Equal([other], await w.OpenDraftIds(w.Admin, w.OtherBranch));
        Assert.Subset(new HashSet<long>(await w.OpenDraftIds(w.Admin)), new HashSet<long> { own, other });
    }

    [Fact]
    public async Task Empty_lists_are_rejected_on_create_and_edit()
    {
        var (w, product, customer) = await Arrange();
        var id = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));

        await Rejection.With(HttpStatusCode.BadRequest, () => w.Seller.Send(w.Draft([], customer)));
        await Rejection.With(HttpStatusCode.BadRequest, () => w.Seller.Send(World.EditDraft(id, w.Draft([], customer))));
        Assert.Single((await w.Seller.Send(new GetSaleDraftByIdQuery(id))).Items);
    }

    [Theory]
    [InlineData(0, 100, 100)]
    [InlineData(-1, 100, 100)]
    [InlineData(2, 0, 100)]
    [InlineData(2, -100, 100)]
    [InlineData(2, 100, 0)]
    [InlineData(2, 100, -5)]
    [InlineData(2, 100, 201)]
    public async Task Malformed_rows_are_rejected_on_create_and_edit(int rolls, int length, int total)
    {
        var (w, product, customer) = await Arrange();
        var id = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        var malformed = w.Draft([new Line(product, rolls, length, Length: total)], customer);

        await Rejection.With(HttpStatusCode.BadRequest, () => w.Seller.Send(malformed));
        await Rejection.With(HttpStatusCode.BadRequest, () => w.Seller.Send(World.EditDraft(id, malformed)));

        var items = (await w.Seller.Send(new GetSaleDraftByIdQuery(id))).Items;
        Assert.Equal([2], items.Select(item => item.RollCount));
    }

    [Fact]
    public async Task A_missing_product_customer_or_list_is_not_found()
    {
        var (w, product, customer) = await Arrange();
        var id = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        const long missing = 999_999_999;
        var withMissingProduct = w.Draft([new Line(missing, 2, 100)], customer);
        var withMissingCustomer = w.Draft([new Line(product, 2, 100)], missing);

        await Rejection.With(HttpStatusCode.NotFound, () => w.Seller.Send(withMissingProduct));
        await Rejection.With(HttpStatusCode.NotFound, () => w.Seller.Send(withMissingCustomer));
        await Rejection.With(HttpStatusCode.NotFound, () => w.Seller.Send(World.EditDraft(id, withMissingProduct)));
        await Rejection.With(HttpStatusCode.NotFound, () => w.Seller.Send(World.EditDraft(id, withMissingCustomer)));
        await Rejection.With(HttpStatusCode.NotFound, () => w.Seller.Send(World.EditDraft(missing, withMissingCustomer with { CustomerId = customer })));
        await Rejection.With(HttpStatusCode.NotFound, () => w.Seller.Send(new CancelSaleDraftCommand(missing)));
        await Rejection.With(HttpStatusCode.NotFound, () => w.Seller.Send(new GetSaleDraftByIdQuery(missing)));
        Assert.Equal(2, Assert.Single((await w.Seller.Send(new GetSaleDraftByIdQuery(id))).Items).RollCount);
    }

    [Fact]
    public async Task A_cancelled_list_cannot_be_edited_or_cancelled_again()
    {
        var (w, product, customer) = await Arrange();
        var id = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        await w.Seller.Send(new CancelSaleDraftCommand(id));

        await Rejection.With(HttpStatusCode.Conflict, () => w.Seller.Send(World.EditDraft(id, w.Draft([new Line(product, 5, 100)], customer))));
        await Rejection.With(HttpStatusCode.Conflict, () => w.Seller.Send(new CancelSaleDraftCommand(id)));
    }

    [Fact]
    public async Task A_confirmed_list_cannot_be_edited_or_cancelled()
    {
        var (w, product, customer) = await Arrange();
        var id = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        await w.Manager.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]) with { SaleDraftId = id });

        await Rejection.With(HttpStatusCode.Conflict, () => w.Seller.Send(World.EditDraft(id, w.Draft([new Line(product, 5, 100)], customer))));
        await Rejection.With(HttpStatusCode.Conflict, () => w.Manager.Send(new CancelSaleDraftCommand(id)));
        Assert.Equal(SaleDraftStatus.Confirmed, (await w.Seller.Send(new GetSaleDraftByIdQuery(id))).Status);
    }

    [Fact]
    public async Task A_stranger_gets_forbidden_on_any_list_whatever_its_state()
    {
        var (w, product, customer) = await Arrange();
        var stranger = w.As(TestUser.Of(UserRole.Seller, w.Branch));
        var open = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        var cancelled = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        await w.Seller.Send(new CancelSaleDraftCommand(cancelled));
        var confirmed = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        await w.Manager.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]) with { SaleDraftId = confirmed });

        foreach (var id in new[] { open, cancelled, confirmed })
            foreach (var outsider in new[] { stranger, w.OtherManager, w.OtherSeller })
            {
                await Rejection.With(HttpStatusCode.Forbidden, () => outsider.Send(World.EditDraft(id, w.Draft([new Line(product, 5, 100)], customer))));
                await Rejection.With(HttpStatusCode.Forbidden, () => outsider.Send(new CancelSaleDraftCommand(id)));
            }

        Assert.Equal(SaleDraftStatus.Open, (await w.Seller.Send(new GetSaleDraftByIdQuery(open))).Status);
    }

    [Fact]
    public async Task A_product_category_or_customer_used_by_a_list_cannot_be_deleted()
    {
        var (w, product, customer) = await Arrange();
        var id = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));

        await Rejection.Conflicting(() => w.Admin.Send(new DeleteProductCommand(product)));
        await Rejection.Conflicting(() => w.Admin.Send(new DeleteCategoryCommand(w.CategoryId)));
        await Rejection.Conflicting(() => w.Admin.Send(new DeleteCustomerCommand(customer)));

        var draft = await w.Seller.Send(new GetSaleDraftByIdQuery(id));
        var item = Assert.Single(draft.Items);
        Assert.Equal(product, item.ProductId);
        Assert.Equal(2, item.RollCount);
        Assert.Equal(customer, draft.CustomerId);
    }

    [Fact]
    public async Task A_list_dated_with_a_local_offset_is_accepted_and_keeps_its_moment()
    {
        var (w, product, customer) = await Arrange();
        var created = new DateTimeOffset(2026, 10, 10, 9, 30, 0, TimeSpan.FromHours(5));
        var edited = new DateTimeOffset(2026, 10, 11, 18, 0, 0, TimeSpan.FromHours(5));

        var id = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer) with { Date = created });
        Assert.Equal(created.UtcDateTime, (await w.Seller.Send(new GetSaleDraftByIdQuery(id))).Date.UtcDateTime);

        await w.Seller.Send(World.EditDraft(id, w.Draft([new Line(product, 2, 100)], customer) with { Date = edited }));
        Assert.Equal(edited.UtcDateTime, (await w.Seller.Send(new GetSaleDraftByIdQuery(id))).Date.UtcDateTime);
    }
}
