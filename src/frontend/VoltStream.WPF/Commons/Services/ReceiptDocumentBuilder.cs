namespace VoltStream.WPF.Commons.Services;

using ApiServices.Models.Responses;
using System.Windows.Documents;
using VoltStream.WPF.Commons.Localization;

public enum ReceiptMode { Money, Weight }

public static class ReceiptDocumentBuilder
{
    public static FixedDocument Build(ReceiptResponse receipt, ReceiptMode mode)
        => ReportService.BuildFixedDocument(Define(receipt, mode));

    public static void Preview(ReceiptResponse receipt, bool allowMoney)
    {
        bool money = allowMoney && receipt.HasPrices;

        IReadOnlyList<(string Label, Func<FixedDocument> Build)>? variants = money
            ?
            [
                (TranslationSource.T("Receipt.ModeMoney"), () => Build(receipt, ReceiptMode.Money)),
                (TranslationSource.T("Receipt.ModeWeight"), () => Build(receipt, ReceiptMode.Weight))
            ]
            : null;

        var document = Build(receipt, money ? ReceiptMode.Money : ReceiptMode.Weight);
        ReportService.Preview(document, FileName(receipt), Title(receipt), variants);
    }

    public static string FileName(ReceiptResponse receipt) => $"Chek-{receipt.SaleId}";

    public static string Title(ReceiptResponse receipt)
        => string.Format(TranslationSource.T("Receipt.Title"), receipt.SaleId);

    private const double ContentWidth = 733.7;

    private static ReportDefinition<ReceiptItemResponse> Define(ReceiptResponse receipt, ReceiptMode mode)
    {
        bool money = mode == ReceiptMode.Money && receipt.HasPrices;

        List<ReportColumn<ReceiptItemResponse>> columns =
        [
            new() { Header = TranslationSource.T("Receipt.Product"), Width = ContentWidth - (money ? 495 : 405), Value = Product },
            new() { Header = TranslationSource.T("Receipt.PerRoll"), Width = 70, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = i => i.LengthPerRoll },
            new() { Header = TranslationSource.T("Receipt.RollCount"), Width = 70, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = i => i.RollCount },
            new() { Header = TranslationSource.T("Receipt.TotalLength"), Width = 85, Align = ReportAlign.Right, Value = i => $"{i.TotalLength:N0} {i.Unit}" }
        ];

        List<ReportTotalCell> totals =
        [
            new() { Column = 2, Value = receipt.TotalRolls, Format = "N0" },
            new() { Column = 3, Value = $"{receipt.TotalLength:N0}" }
        ];

        if (!money)
            columns.Add(new() { Header = TranslationSource.T("Receipt.WeightPerUnit"), Width = 95, Align = ReportAlign.Right, IsNumber = true, Format = "N3", Value = i => i.WeightPerUnit });

        columns.Add(new() { Header = TranslationSource.T("Receipt.Weight"), Width = 85, Align = ReportAlign.Right, IsNumber = true, Format = "N3", Value = i => i.Weight });
        totals.Add(new() { Column = columns.Count - 1, Value = receipt.TotalWeight, Format = "N3" });

        if (money)
        {
            columns.Add(new() { Header = TranslationSource.T("Receipt.Price"), Width = 90, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = i => i.UnitPrice });
            columns.Add(new() { Header = TranslationSource.T("Receipt.Amount"), Width = 105, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = i => i.TotalAmount });
            totals.Add(new() { Column = columns.Count - 1, Value = receipt.Amount, Format = "N0" });
        }

        return new ReportDefinition<ReceiptItemResponse>
        {
            Title = Title(receipt),
            Subtitle = Subtitle(receipt, money),
            SheetName = Title(receipt),
            FileName = FileName(receipt),
            Rows = receipt.Items,
            Columns = columns,
            Totals = new ReportTotals
            {
                Label = TranslationSource.T("Receipt.Total"),
                LabelSpan = 2,
                Cells = totals
            }
        };
    }

    private static string Product(ReceiptItemResponse item)
        => string.IsNullOrWhiteSpace(item.Category) ? item.Product ?? string.Empty : $"{item.Category} {item.Product}";

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
