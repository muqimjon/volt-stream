namespace VoltStream.Application.Tests.Drafts;

using System.Net;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.PrintJobs.Commands;
using VoltStream.Application.Features.PrintJobs.DTOs;
using VoltStream.Application.Features.PrintJobs.Queries;
using VoltStream.Application.Features.SaleDrafts.Commands;
using VoltStream.Application.Features.SaleDrafts.Queries;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Domain.Enums;

[Collection(DatabaseCollection.Name)]
public sealed class SaleDraftPrintTests(PostgresFixture db)
{
    private async Task<(World World, long Product, long Customer)> Arrange()
    {
        var world = await db.NewWorld();
        var product = await world.NewProduct();
        await world.Supply(product, 10, 100, 5_000);

        return (world, product, await world.NewCustomer());
    }

    private static Task<PrintJobDto> Print(Actor actor, long draft, bool force = false)
        => actor.Send(new CreatePrintJobCommand(SaleDraftId: draft, Force: force));

    [Fact]
    public async Task Printing_a_list_needs_drafts_print()
    {
        var (w, product, customer) = await Arrange();
        var owner = await w.NewUser(UserRole.Seller, w.Branch);
        var draft = await owner.Send(w.Draft([new Line(product, 2, 100)], customer));
        Actor[] actors =
        [
            owner,
            await w.NewUser(UserRole.Manager, w.Branch),
            await w.NewUser(UserRole.Cashier, w.Branch),
            await w.NewUser(UserRole.Seller, w.Branch, Permissions.DraftsConfirm)
        ];

        foreach (var actor in actors)
            await Rejection.With(HttpStatusCode.Forbidden, () => Print(actor, draft));

        Assert.DoesNotContain(w.Db.Notifier.Created, job => job.SaleDraftId == draft);
    }

    [Fact]
    public async Task A_list_is_printed_as_a_pending_job_for_its_branch()
    {
        var (w, product, customer) = await Arrange();
        var owner = await w.NewUser(UserRole.Seller, w.Branch, Permissions.DraftsPrint);
        var draft = await owner.Send(w.Draft([new Line(product, 2, 100)], customer));

        var job = await Print(owner, draft);

        Assert.Equal(draft, job.SaleDraftId);
        Assert.Null(job.SaleId);
        Assert.Equal(PrintJobStatus.Pending, job.Status);
        Assert.Equal(w.Branch, job.BranchId);
        Assert.Equal(owner.User.UserId, job.RequestedByUserId);
        Assert.Contains(w.Db.Notifier.Created, created => created.Id == job.Id);
        var agent = w.As(TestUser.With(w.Branch, Permissions.PrintAgent));
        Assert.Contains(job.Id, (await agent.Send(new GetPendingPrintJobsQuery())).Select(pending => pending.Id));
    }

    [Fact]
    public async Task Exactly_one_of_sale_and_list_is_required()
    {
        var (w, product, customer) = await Arrange();
        var printer = await w.NewUser(UserRole.Manager, w.Branch, Permissions.DraftsPrint);
        var draft = await printer.Send(w.Draft([new Line(product, 2, 100)], customer));
        var sale = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 5_000)]));

        await Rejection.With(HttpStatusCode.BadRequest, () => printer.Send(new CreatePrintJobCommand(SaleId: sale, SaleDraftId: draft)));
        await Rejection.With(HttpStatusCode.BadRequest, () => printer.Send(new CreatePrintJobCommand()));
        Assert.DoesNotContain(w.Db.Notifier.Created, job => job.SaleDraftId == draft || job.SaleId == sale);
    }

    [Fact]
    public async Task An_active_job_blocks_another_and_forcing_needs_drafts_confirm()
    {
        var (w, product, customer) = await Arrange();
        var owner = await w.NewUser(UserRole.Seller, w.Branch, Permissions.DraftsPrint);
        var confirmer = await w.NewUser(UserRole.Manager, w.Branch, Permissions.DraftsPrint);
        var draft = await owner.Send(w.Draft([new Line(product, 2, 100)], customer));
        var first = await Print(owner, draft);

        await Rejection.With(HttpStatusCode.Conflict, () => Print(owner, draft));
        await Rejection.With(HttpStatusCode.Forbidden, () => Print(owner, draft, force: true));
        var forced = await Print(confirmer, draft, force: true);

        Assert.NotEqual(first.Id, forced.Id);
        Assert.Equal(PrintJobStatus.Pending, forced.Status);
    }

    [Fact]
    public async Task Only_open_lists_are_printed()
    {
        var (w, product, customer) = await Arrange();
        var confirmer = await w.NewUser(UserRole.Manager, w.Branch, Permissions.DraftsPrint);
        var confirmed = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        var cancelled = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));
        await confirmer.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]) with { SaleDraftId = confirmed });
        await w.Seller.Send(new CancelSaleDraftCommand(cancelled));

        await Rejection.With(HttpStatusCode.Conflict, () => Print(confirmer, confirmed));
        await Rejection.With(HttpStatusCode.Conflict, () => Print(confirmer, cancelled));
        await Rejection.With(HttpStatusCode.NotFound, () => Print(confirmer, 999_999_999));
    }

    [Fact]
    public async Task Printing_follows_who_may_see_the_list()
    {
        var (w, product, customer) = await Arrange();
        var owner = await w.NewUser(UserRole.Seller, w.Branch, Permissions.DraftsPrint);
        var colleague = await w.NewUser(UserRole.Seller, w.Branch, Permissions.DraftsPrint);
        var confirmer = await w.NewUser(UserRole.Manager, w.Branch, Permissions.DraftsPrint);
        var outsider = await w.NewUser(UserRole.Manager, w.OtherBranch, Permissions.DraftsPrint);
        var draft = await owner.Send(w.Draft([new Line(product, 2, 100)], customer));

        await Rejection.With(HttpStatusCode.Forbidden, () => Print(colleague, draft));
        await Rejection.With(HttpStatusCode.Forbidden, () => Print(outsider, draft));

        Assert.Equal(draft, (await Print(confirmer, draft)).SaleDraftId);
    }

    [Fact]
    public async Task Only_the_branch_agent_takes_the_list()
    {
        var (w, product, customer) = await Arrange();
        var owner = await w.NewUser(UserRole.Seller, w.Branch, Permissions.DraftsPrint);
        var colleague = w.As(TestUser.Of(UserRole.Seller, w.Branch));
        var agent = w.As(TestUser.With(w.Branch, Permissions.PrintAgent));
        var otherAgent = w.As(TestUser.With(w.OtherBranch, Permissions.PrintAgent));
        var draft = await owner.Send(w.Draft([new Line(product, 2, 100)], customer));
        var job = await Print(owner, draft);

        await Rejection.With(HttpStatusCode.Forbidden, () => otherAgent.Send(new ClaimPrintJobCommand(job.Id)));
        await Rejection.With(HttpStatusCode.Forbidden, () => otherAgent.Send(new GetSaleDraftReceiptQuery(draft)));

        var claimed = await agent.Send(new ClaimPrintJobCommand(job.Id, "Printer"));
        Assert.Equal(PrintJobStatus.Printing, claimed.Status);
        Assert.Equal(draft, (await agent.Send(new GetSaleDraftReceiptQuery(draft))).SaleDraftId);

        Assert.Equal(draft, (await owner.Send(new GetSaleDraftReceiptQuery(draft))).SaleDraftId);
        await Rejection.With(HttpStatusCode.Forbidden, () => colleague.Send(new GetSaleDraftReceiptQuery(draft)));
        await Rejection.With(HttpStatusCode.Forbidden, () => w.Cashier.Send(new GetSaleDraftReceiptQuery(draft)));
    }

    [Fact]
    public async Task The_list_receipt_has_no_prices_and_the_right_composition()
    {
        var (w, _, customer) = await Arrange();
        var plain = await w.NewProduct();
        var weighed = await w.NewProduct(weightTracked: true, weightPerUnit: 0.25m);
        var agent = w.As(TestUser.With(w.Branch, Permissions.PrintAgent));
        var draft = await w.Seller.Send(w.Draft([
            new Line(plain, 2, 50),
            new Line(weighed, 2, 100, Length: 150),
            new Line(plain, 1, 100, Length: 70)], customer));

        var receipt = await agent.Send(new GetSaleDraftReceiptQuery(draft));

        Assert.Equal(draft, receipt.SaleDraftId);
        Assert.Equal(0, receipt.SaleId);
        Assert.NotNull(receipt.Customer);
        Assert.False(receipt.HasPrices);
        Assert.Null(receipt.Amount);
        Assert.Null(receipt.Discount);
        Assert.All(receipt.Items, item =>
        {
            Assert.Null(item.UnitPrice);
            Assert.Null(item.TotalAmount);
        });
        Assert.Equal(["2 × 50 + 1 × 70", "1 × 100 + 1 × 50"], receipt.Items.Select(item => item.Composition));
        Assert.Equal([3, 2], receipt.Items.Select(item => item.RollCount));
        Assert.Equal([0m, 100m], receipt.Items.Select(item => item.LengthPerRoll));
        Assert.Equal(5, receipt.TotalRolls);
        Assert.Equal(100 + 70 + 150, receipt.TotalLength);
        Assert.Equal(150 * 0.25m, receipt.TotalWeight);
    }

    [Fact]
    public async Task Printing_a_sale_still_works_and_stays_a_sale_job()
    {
        var (w, product, customer) = await Arrange();
        var seller = await w.NewUser(UserRole.Seller, w.Branch);
        var sale = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 5_000)]));

        var job = await seller.Send(new CreatePrintJobCommand(SaleId: sale));

        Assert.Equal(sale, job.SaleId);
        Assert.Null(job.SaleDraftId);
        await Rejection.With(HttpStatusCode.Conflict, () => seller.Send(new CreatePrintJobCommand(SaleId: sale)));
        await Rejection.With(HttpStatusCode.Forbidden, () => seller.Send(new CreatePrintJobCommand(SaleId: sale, Force: true)));
    }

    [Fact]
    public async Task Cancelling_a_list_cancels_its_active_print_jobs_and_nobody_elses()
    {
        var (w, product, customer) = await Arrange();
        var owner = await w.NewUser(UserRole.Seller, w.Branch, Permissions.DraftsPrint);
        var confirmer = await w.NewUser(UserRole.Manager, w.Branch, Permissions.DraftsPrint);
        var agent = w.As(TestUser.With(w.Branch, Permissions.PrintAgent));
        var draft = await owner.Send(w.Draft([new Line(product, 2, 100)], customer));
        var bystander = await owner.Send(w.Draft([new Line(product, 1, 100)], customer));
        var pending = await Print(owner, draft);
        var printing = await Print(confirmer, draft, force: true);
        await agent.Send(new ClaimPrintJobCommand(printing.Id));
        var untouched = await Print(owner, bystander);

        await owner.Send(new CancelSaleDraftCommand(draft));

        Assert.Equal(PrintJobStatus.Cancelled, (await owner.Send(new GetPrintJobByIdQuery(pending.Id))).Status);
        Assert.Equal(PrintJobStatus.Cancelled, (await owner.Send(new GetPrintJobByIdQuery(printing.Id))).Status);
        Assert.Equal(PrintJobStatus.Pending, (await owner.Send(new GetPrintJobByIdQuery(untouched.Id))).Status);
        Assert.All([pending.Id, printing.Id], id => Assert.Contains(w.Db.Notifier.Changed, job => job.Id == id && job.Status == PrintJobStatus.Cancelled));
    }
}
