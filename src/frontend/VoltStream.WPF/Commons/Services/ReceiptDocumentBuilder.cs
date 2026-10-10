namespace VoltStream.WPF.Commons.Services;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using VoltStream.WPF.Commons.Dialogs;
using VoltStream.WPF.Commons.Enums;
using VoltStream.WPF.Commons.Localization;

public enum ReceiptMode { Money, Weight }

public enum ReceiptLayout { Expanded, Compact }

public static class ReceiptDocumentBuilder
{
    public static FixedDocument Build(ReceiptResponse receipt, ReceiptMode mode, ReceiptLayout layout, string paper = "A4")
        => ReportService.BuildFixedDocument(Define(receipt, mode, layout, ReportService.PaperSize(paper)));

    public static void Preview(ReceiptResponse receipt, bool allowMoney, IServiceProvider services)
    {
        bool money = allowMoney && receipt.HasPrices;
        var mode = money ? ReceiptMode.Money : ReceiptMode.Weight;
        var layout = ReceiptLayout.Expanded;

        FixedDocument Rebuild() => Build(receipt, mode, layout);

        var toggleGroups = new List<IReadOnlyList<(string Label, Func<FixedDocument> Build, bool IsDefault)>>();

        if (money)
            toggleGroups.Add(
            [
                (TranslationSource.T("Receipt.ModeMoney"), () => { mode = ReceiptMode.Money; return Rebuild(); }, true),
                (TranslationSource.T("Receipt.ModeWeight"), () => { mode = ReceiptMode.Weight; return Rebuild(); }, false)
            ]);

        toggleGroups.Add(
        [
            (TranslationSource.T("Receipt.LayoutCompact"), () => { layout = ReceiptLayout.Compact; return Rebuild(); }, false),
            (TranslationSource.T("Receipt.LayoutExpanded"), () => { layout = ReceiptLayout.Expanded; return Rebuild(); }, true)
        ]);

        IReadOnlyList<(string Label, Func<Button, Task> Run)>? actions =
            services.GetRequiredService<ISessionService>().Has(receipt.SaleDraftId is null ? "print.request" : "drafts.print")
                ? [(TranslationSource.T("Receipt.SendToBranch"), button => SendToBranchAsync(receipt, services, button))]
                : null;

        ReportService.Preview(Rebuild(), FileName(receipt), Title(receipt), toggleGroups, actions);
    }

    private static async Task SendToBranchAsync(ReceiptResponse receipt, IServiceProvider services, Button button)
    {
        if (!services.GetRequiredService<ISessionService>().Has("branches.all"))
        {
            await SendAsync(receipt, services, null, button);
            return;
        }

        var branchContext = services.GetRequiredService<BranchContext>();
        if (branchContext.Branches.Count == 0)
            await branchContext.LoadAsync();

        var branches = branchContext.Branches.Where(b => b.Id > 0).ToList();
        if (branches.Count == 0)
        {
            await SendAsync(receipt, services, null, button);
            return;
        }

        var own = branches.FirstOrDefault(b => b.Name == receipt.Branch);
        var menu = new ContextMenu { PlacementTarget = button, Placement = PlacementMode.Bottom };
        menu.SetResourceReference(Control.BackgroundProperty, "Surface");
        menu.SetResourceReference(Control.BorderBrushProperty, "Border");

        foreach (var branch in own is null ? branches : [own, .. branches.Where(b => b != own)])
        {
            var entry = new MenuItem
            {
                Header = branch.Name,
                FontWeight = branch == own ? FontWeights.Bold : FontWeights.Normal,
                IsChecked = branch == own
            };
            entry.Click += async (_, _) => await SendAsync(receipt, services, branch.Id, button);
            menu.Items.Add(entry);
        }

        menu.IsOpen = true;
    }

    private static async Task SendAsync(ReceiptResponse receipt, IServiceProvider services, long? branchId, Button button)
    {
        var response = await services.GetRequiredService<IPrintJobsApi>()
            .CreateAsync(new PrintJobRequest
            {
                SaleId = receipt.SaleDraftId is null ? receipt.SaleId : null,
                SaleDraftId = receipt.SaleDraftId,
                Copies = 1,
                BranchId = branchId
            }).Handle();

        var owner = Window.GetWindow(button);
        var title = TranslationSource.T("Receipt.SendToBranch");

        if (response.IsSuccess)
            AppDialog.Info(TranslationSource.T("Receipt.SentToBranch"), title, owner);
        else
            AppDialog.Warning(
                string.IsNullOrWhiteSpace(response.Message) ? TranslationSource.T("Receipt.SendFailed") : response.Message,
                title, owner);
    }

    public static string FileName(ReceiptResponse receipt)
        => receipt.SaleDraftId is long draftId ? $"Yuk-royxati-{draftId}" : $"Chek-{receipt.SaleId}";

    public static string Title(ReceiptResponse receipt)
        => receipt.SaleDraftId is long draftId
            ? string.Format(TranslationSource.T("Receipt.DraftTitle"), draftId)
            : string.Format(TranslationSource.T("Receipt.Title"), receipt.SaleId);

    private static ReportDefinition<ReceiptItemResponse> Define(ReceiptResponse receipt, ReceiptMode mode, ReceiptLayout layout, Size page)
    {
        bool money = mode == ReceiptMode.Money && receipt.HasPrices;
        bool expanded = layout == ReceiptLayout.Expanded;
        bool hasComposition = receipt.Items.Any(i => !string.IsNullOrWhiteSpace(i.Composition));
        double contentWidth = ReportService.ContentWidth(page);

        List<ReportColumn<ReceiptItemResponse>> rest = [];

        if (!hasComposition)
            rest.Add(new() { Header = TranslationSource.T("Receipt.PerRoll"), Width = 70, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = i => i.LengthPerRoll });

        rest.Add(new() { Header = TranslationSource.T("Receipt.RollCount"), Width = 70, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = i => i.RollCount });

        int rollCountColumn = rest.Count;

        if (expanded)
            rest.Add(new() { Header = TranslationSource.T("Receipt.Composition"), Width = 165, Value = ExpandedComposition });
        else
            rest.Add(new() { Header = TranslationSource.T("Receipt.TotalLength"), Width = 85, Align = ReportAlign.Right, Value = i => $"{i.TotalLength:N0} {i.Unit}" });

        List<ReportTotalCell> totals =
        [
            new() { Column = rollCountColumn, Value = receipt.TotalRolls, Format = "N0" },
            new() { Column = rollCountColumn + 1, Value = $"{receipt.TotalLength:N0}" }
        ];

        if (money)
        {
            rest.Add(new() { Header = TranslationSource.T("Receipt.Price"), Width = 90, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = i => (object?)i.UnitPrice ?? "—" });
            rest.Add(new() { Header = TranslationSource.T("Receipt.Amount"), Width = 105, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = i => i.TotalAmount });
            totals.Add(new() { Column = rest.Count, Value = receipt.Amount, Format = "N0" });
        }
        else
        {
            rest.Add(new() { Header = TranslationSource.T("Receipt.WeightPerUnit"), Width = 95, Align = ReportAlign.Right, IsNumber = true, Format = "N3", Value = i => i.WeightPerUnit });
            rest.Add(new() { Header = TranslationSource.T("Receipt.Weight"), Width = 85, Align = ReportAlign.Right, IsNumber = true, Format = "N3", Value = i => i.Weight });
            totals.Add(new() { Column = rest.Count, Value = receipt.TotalWeight, Format = "N3" });
        }

        var product = new ReportColumn<ReceiptItemResponse>
        {
            Header = TranslationSource.T("Receipt.Product"),
            Width = Math.Max(80, contentWidth - rest.Sum(c => c.Width)),
            Value = Product
        };

        return new ReportDefinition<ReceiptItemResponse>
        {
            Title = Title(receipt),
            Subtitle = Subtitle(receipt, money),
            SheetName = Title(receipt),
            FileName = FileName(receipt),
            Rows = receipt.Items,
            Columns = [product, .. rest],
            PageSize = page,
            Totals = new ReportTotals
            {
                Label = TranslationSource.T("Receipt.Total"),
                LabelSpan = rollCountColumn,
                Cells = totals
            }
        };
    }

    private static string Product(ReceiptItemResponse item)
        => string.IsNullOrWhiteSpace(item.Category) ? item.Product ?? string.Empty : $"{item.Category} {item.Product}";

    private static string ExpandedComposition(ReceiptItemResponse item)
    {
        if (item.Rolls.Count == 0)
            return item.Composition ?? string.Empty;

        if (item.Rolls.Count == 1)
        {
            var r = item.Rolls[0];
            return $"{r.Count} × {r.Length:0.###} = {item.TotalLength:0.###} {item.Unit}";
        }

        var lines = item.Rolls.Select(r => $"{r.Count} × {r.Length:0.###} {item.Unit}");
        return string.Join("\n", lines) + $"\n{TranslationSource.T("Common.Total")}: {item.TotalLength:0.###} {item.Unit}";
    }

    private static string Subtitle(ReceiptResponse receipt, bool money)
        => string.Join("  |  ", new[]
        {
            receipt.Branch,
            receipt.Date.LocalDateTime.ToString("dd.MM.yyyy HH:mm"),
            receipt.Customer,
            receipt.Operator,
            money && receipt.Discount is > 0 ? $"{TranslationSource.T("Receipt.Discount")}: {receipt.Discount:N0}" : null
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
