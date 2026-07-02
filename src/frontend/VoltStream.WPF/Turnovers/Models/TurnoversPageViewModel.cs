namespace VoltStream.WPF.Turnovers.Models;

using ApiServices.Enums;
using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using ApiServices.Models.Responses;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Messages;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.ViewModels;
using VoltStream.WPF.Payments.Views;
using VoltStream.WPF.Sales.Views;

public partial class TurnoversPageViewModel : ViewModelBase
{
    private readonly ICustomersApi customersApi;
    private readonly ICustomerOperationsApi customerOperationsApi;
    private readonly IMapper mapper;
    private readonly IServiceProvider services;
    private readonly INavigationService navigationService;

    private static readonly Brush PdfAccent = new SolidColorBrush(Color.FromRgb(0x4F, 0x46, 0xE5));
    private static readonly Brush PdfInk = new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x37));
    private static readonly Brush PdfMuted = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
    private static readonly Brush PdfLine = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB));
    private static readonly Brush PdfHeaderBg = new SolidColorBrush(Color.FromRgb(0xEE, 0xF0, 0xFD));
    private static readonly Brush PdfDanger = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));

    public TurnoversPageViewModel(IServiceProvider services, INavigationService navigationService)
    {
        this.services = services;
        this.navigationService = navigationService;
        customersApi = services.GetRequiredService<ICustomersApi>();
        customerOperationsApi = services.GetRequiredService<ICustomerOperationsApi>();
        mapper = services.GetRequiredService<IMapper>();

        Pagination = new PaginationViewModel(LoadPageAsync);

        WeakReferenceMessenger.Default.Register<EntityUpdatedMessage<string>>(this, (r, m) =>
        {
            if (m.Value == "OperationUpdated")
                _ = ReloadAsync();
        });

        _ = LoadInitialDataAsync();
    }

    public PaginationViewModel Pagination { get; }

    [ObservableProperty] private CustomerResponse? selectedCustomer;
    [ObservableProperty] private ObservableCollection<CustomerResponse> customers = [];
    [ObservableProperty] private ObservableCollection<CustomerOperationForDisplayViewModel> pagedCustomerOperationsForDisplay = [];
    [ObservableProperty] private CustomerOperationForDisplayViewModel? selectedItem;
    [ObservableProperty] private DateTime beginDate = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime endDate = DateTime.Today;
    [ObservableProperty] private decimal? beginBalance;
    [ObservableProperty] private decimal? lastBalance;

    #region Property Changes

    partial void OnSelectedCustomerChanged(CustomerResponse? value) => _ = ReloadAsync();

    partial void OnBeginDateChanged(DateTime value) => _ = ReloadAsync();

    partial void OnEndDateChanged(DateTime value) => _ = ReloadAsync();

    #endregion Property Changes

    #region Load Data

    private async Task LoadInitialDataAsync()
    {
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        var response = await customersApi.GetAllAsync().Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess)
            Customers = mapper.Map<ObservableCollection<CustomerResponse>>(response.Data!);
        else Error = response.Message ?? TranslationSource.T("Turnovers.CustomersLoadError");
    }

    private async Task ReloadAsync()
    {
        Pagination.Reset();
        await LoadSummaryAsync();
        await LoadPageAsync();
    }

    private async Task LoadSummaryAsync()
    {
        if (SelectedCustomer is null)
        {
            BeginBalance = null;
            LastBalance = null;
            return;
        }

        var response = await customerOperationsApi.GetByCustomerId(SelectedCustomer.Id, BeginDate, EndDate)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess && response.Data is not null)
        {
            BeginBalance = response.Data.BeginBalance;
            LastBalance = response.Data.EndBalance;
        }
        else
        {
            BeginBalance = null;
            LastBalance = null;
        }
    }

    private async Task LoadPageAsync()
    {
        if (SelectedCustomer is null)
        {
            PagedCustomerOperationsForDisplay = [];
            Pagination.SetTotal(0);
            return;
        }

        var request = BuildFilters();
        request.Page = Pagination.Page;
        request.PageSize = Pagination.PageSize;

        Response<List<CustomerOperationResponse>> response;
        using (PagingScope.Begin())
        {
            response = await customerOperationsApi.Filter(request).Handle(l => IsLoading = l);
            if (response.IsSuccess) Pagination.Apply(PagingScope.Result);
        }

        if (!response.IsSuccess)
        {
            Error = response.Message ?? TranslationSource.T("Turnovers.OperationsLoadError");
            return;
        }

        PagedCustomerOperationsForDisplay = new ObservableCollection<CustomerOperationForDisplayViewModel>(response.Data!.Select(Map));
    }

    private async Task<List<CustomerOperationForDisplayViewModel>> LoadAllOperationsAsync()
    {
        if (SelectedCustomer is null) return [];
        var response = await customerOperationsApi.Filter(BuildFilters()).Handle(l => IsLoading = l);
        return response.IsSuccess ? response.Data!.Select(Map).ToList() : [];
    }

    private FilteringRequest BuildFilters()
    {
        var filters = new Dictionary<string, List<string>>
        {
            ["Date"] = [$">={BeginDate:yyyy-MM-dd}", $"<{EndDate.AddDays(1):yyyy-MM-dd}"]
        };

        if (SelectedCustomer is not null)
            filters["CustomerId"] = [$"={SelectedCustomer.Id}"];

        return new FilteringRequest { Filters = filters, SortBy = "Date" };
    }

    private CustomerOperationForDisplayViewModel Map(CustomerOperationResponse op)
    {
        decimal debit = 0;
        decimal credit = 0;

        if (op.OperationType == OperationType.Payment)
        {
            if (op.Amount < 0)
                debit = Math.Abs(op.Amount);
            else
                credit = op.Amount;
        }
        else if (op.OperationType == OperationType.Sale)
        {
            debit = Math.Abs(op.Amount);
        }
        else if (op.OperationType == OperationType.Discount)
        {
            credit = op.Amount;
        }

        return new CustomerOperationForDisplayViewModel
        {
            Id = op.Id,
            Date = op.Date.LocalDateTime,
            Customer = SelectedCustomer?.Name ?? TranslationSource.T("Turnovers.Unknown"),
            Debit = debit,
            Credit = credit,
            Description = op.Description,
            OperationType = op.OperationType
        };
    }

    #endregion Load Data

    #region Commands

    [RelayCommand]
    private async Task Delete(CustomerOperationForDisplayViewModel? operation)
    {
        if (operation is null)
        {
            Warning = TranslationSource.T("Turnovers.NoOperationToDelete");
            return;
        }

        var result = MessageBox.Show(
            $"{TranslationSource.T("Turnovers.ConfirmDeleteOperation")}\n\n" +
            $"{TranslationSource.T("Turnovers.DateLabel")} {operation.Date:dd.MM.yyyy}\n" +
            $"{TranslationSource.T("Turnovers.Debit")}: {operation.Debit:N2}\n" +
            $"{TranslationSource.T("Turnovers.Credit")}: {operation.Credit:N2}\n" +
            $"{TranslationSource.T("Common.Description")}: {operation.Description}",
            TranslationSource.T("Turnovers.ConfirmDeleteTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.No)
            return;

        var response = await customerOperationsApi.Delete(operation.Id)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
        {
            Success = TranslationSource.T("Turnovers.OperationDeleted");
            await ReloadAsync();
        }
        else Error = response.Message ?? TranslationSource.T("Turnovers.OperationDeleteError");
    }

    [RelayCommand]
    private async Task Edit(CustomerOperationForDisplayViewModel? operation)
    {
        if (operation is null)
        {
            Warning = TranslationSource.T("Turnovers.NoOperationToEdit");
            return;
        }

        try
        {
            switch (operation.OperationType)
            {
                case OperationType.Sale:
                    await OpenSaleEditPage(operation.Id);
                    break;

                case OperationType.Payment:
                    await OpenPaymentEditPage(operation.Id);
                    break;

                case OperationType.Discount:
                    Warning = TranslationSource.T("Turnovers.DiscountNotEditable");
                    break;

                default:
                    Warning = TranslationSource.T("Turnovers.UnknownOperationType");
                    break;
            }
        }
        catch (Exception ex)
        {
            Error = $"{TranslationSource.T("Turnovers.OpenEditPageError")}: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SelectedCustomer = null;
        BeginDate = DateTime.Now.AddMonths(-1);
        EndDate = DateTime.Now;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        var operations = await LoadAllOperationsAsync();
        if (operations.Count == 0)
        {
            Info = TranslationSource.T("Turnovers.NoDataToExport");
            return;
        }

        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = TranslationSource.T("Turnovers.ExcelFileFilter"),
                FileName = TranslationSource.T("Turnovers.ExcelFileName")
            };

            if (dialog.ShowDialog() != true) return;

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add(TranslationSource.T("Turnovers.Operations"));

                int row = 1;

                ws.Cell(row, 1).Value = TranslationSource.T("Turnovers.OperationsReportTitle");
                ws.Range($"A{row}:E{row}").Merge().Style
                    .Font.SetBold()
                    .Font.SetFontSize(16)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                row++;

                ws.Cell(row, 1).Value = $"{TranslationSource.T("Turnovers.CustomerLabel")} {SelectedCustomer?.Name.ToUpper() ?? TranslationSource.T("Turnovers.NotSelected")}";
                ws.Range($"A{row}:E{row}").Merge().Style
                    .Font.SetBold()
                    .Font.SetFontSize(14)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                row++;

                ws.Cell(row, 1).Value = string.Format(TranslationSource.T("Turnovers.PeriodRange"), BeginDate.ToString("dd.MM.yyyy"), EndDate.ToString("dd.MM.yyyy"));
                ws.Range($"A{row}:E{row}").Merge().Style
                    .Font.SetBold()
                    .Font.SetFontSize(14)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                row += 2;

                string[] headers = { TranslationSource.T("Common.Date"), TranslationSource.T("Turnovers.Customer"), TranslationSource.T("Turnovers.Debit"), TranslationSource.T("Turnovers.Credit"), TranslationSource.T("Common.Description") };
                for (int i = 0; i < headers.Length; i++)
                    ws.Cell(row, i + 1).Value = headers[i];

                ws.Range($"A{row}:E{row}").Style.Font.Bold = true;
                row++;

                ws.Range($"A{row}:D{row}").Merge();
                ws.Cell(row, 1).Value = TranslationSource.T("Turnovers.BeginBalance");
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 5).Value = BeginBalance?.ToString("N2") ?? "0.00";
                ws.Cell(row, 5).Style.Font.Bold = true;
                ws.Cell(row, 5).Style.Alignment.WrapText = true;
                row++;

                foreach (var item in operations)
                {
                    ws.Cell(row, 1).Value = item.Date.ToString("dd.MM.yyyy");
                    ws.Cell(row, 2).Value = item.Customer;
                    ws.Cell(row, 3).Value = item.Debit;
                    ws.Cell(row, 4).Value = item.Credit;

                    var formattedDescription = string.Join(Environment.NewLine,
                        (item.Description ?? "").Split(';').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)));

                    ws.Cell(row, 5).Value = formattedDescription;
                    ws.Cell(row, 5).Style.Alignment.WrapText = true;

                    row++;
                }

                ws.Range($"A{row}:D{row}").Merge();
                ws.Cell(row, 1).Value = TranslationSource.T("Turnovers.LastBalance");
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 5).Value = LastBalance?.ToString("N2") ?? "0.00";
                ws.Cell(row, 5).Style.Font.Bold = true;
                ws.Cell(row, 5).Style.Alignment.WrapText = true;

                ws.Columns().AdjustToContents();

                workbook.SaveAs(dialog.FileName);
            }

            Success = TranslationSource.T("Turnovers.ExcelSaved");
        }
        catch (Exception ex)
        {
            Error = $"{TranslationSource.T("Turnovers.GenericError")}: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task Print()
    {
        var operations = await LoadAllOperationsAsync();
        if (operations.Count == 0)
        {
            Info = TranslationSource.T("Turnovers.NoDataToPrint");
            return;
        }

        var dlg = new PrintDialog();
        if (dlg.ShowDialog() == true)
            dlg.PrintDocument(CreateFixedDocument(operations).DocumentPaginator, TranslationSource.T("Turnovers.Operations"));
    }

    [RelayCommand]
    private async Task Preview()
    {
        var operations = await LoadAllOperationsAsync();
        if (operations.Count == 0)
        {
            Info = TranslationSource.T("Turnovers.NoDataToPreview");
            return;
        }

        var name = SelectedCustomer?.Name ?? TranslationSource.T("Turnovers.Customer");
        var fileName = $"{name} {BeginDate:dd.MM.yyyy}-{EndDate:dd.MM.yyyy}";
        ReportService.Preview(CreateFixedDocument(operations), fileName, TranslationSource.T("Turnovers.OperationsReportName"));
    }
    #endregion Commands

    #region Private Helpers

    private async Task OpenSaleEditPage(long operationId)
    {
        var saleResponse = await customerOperationsApi.GetById(operationId)
            .Handle(isLoading => IsLoading = isLoading);

        if (!saleResponse.IsSuccess)
        {
            Error = saleResponse.Message ?? TranslationSource.T("Turnovers.SaleDataNotFound");
            return;
        }

        navigationService.Navigate(new SaleEditPage(services, saleResponse.Data.Sale!));
    }

    private async Task OpenPaymentEditPage(long operationId)
    {
        var response = await customerOperationsApi.GetById(operationId)
            .Handle(isLoading => IsLoading = isLoading);

        if (!response.IsSuccess)
        {
            Error = response.Message ?? TranslationSource.T("Turnovers.SaleDataNotFound");
            return;
        }

        navigationService.Navigate(new PaymentEditPage(services, response.Data.Payment!));
    }

    #endregion Private Helpers

    #region PDF Export and Share

    private FixedDocument CreateFixedDocument(List<CustomerOperationForDisplayViewModel> operations)
    {
        var doc = new FixedDocument();
        const double pageWidth = 793.7;
        const double pageHeight = 1122.5;
        const double margin = 25;
        const double approxSingleRowHeight = 25;

        double currentY = 0;
        int pageNumber = 1;
        int currentIndex = 0;
        List<FixedPage> tempPages = [];

        while (currentIndex < operations.Count)
        {
            bool isFirstPage = (pageNumber == 1);
            var page = new FixedPage { Width = pageWidth, Height = pageHeight, Background = Brushes.White };
            var container = new StackPanel { Margin = new Thickness(margin, 30, margin, margin) };

            if (isFirstPage)
            {
                currentY = AddHeaderContent(container, pageNumber, true);
                var beginBalanceBlock = CreateBalanceInfoBlock(TranslationSource.T("Turnovers.BeginBalance"), BeginBalance?.ToString("N2") ?? "0.00", PdfHeaderBg);
                beginBalanceBlock.Margin = new Thickness(0);
                container.Children.Add(beginBalanceBlock);
                currentY += 30;
            }
            else
            {
                currentY = AddHeaderContent(container, pageNumber, false);
            }

            var table = new Grid();
            double[] widths = [80, 547, 116];
            foreach (var w in widths)
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(w) });

            AddRowHeader(table, TranslationSource.T("Common.Date"), TranslationSource.T("Common.Description"), approxSingleRowHeight);

            double footerSpace = approxSingleRowHeight * 4 + 50;
            var opsOnPage = new List<CustomerOperationForDisplayViewModel>();

            int tempIndex = currentIndex;
            while (tempIndex < operations.Count)
            {
                var op = operations[tempIndex];
                double requiredHeight = CalculateOperationRowHeight(op, widths[1]);
                double availableSpace = pageHeight - (margin * 2) - currentY - footerSpace;

                if (requiredHeight > availableSpace && tempIndex > currentIndex) break;

                opsOnPage.Add(op);
                tempIndex++;
                currentY += requiredHeight;
            }

            foreach (var op in opsOnPage)
            {
                AddOperationRow(table, op, widths[1]);
            }

            currentIndex += opsOnPage.Count;
            bool isLastPage = (currentIndex >= operations.Count);

            if (isLastPage)
            {
                decimal totalDebit = operations.Sum(x => x.Debit);
                decimal totalCredit = operations.Sum(x => x.Credit);

                AddRowTotalNew(table, totalCredit, totalDebit, approxSingleRowHeight);

                container.Children.Add(table);

                var lastBalanceBlock = CreateBalanceInfoBlock(TranslationSource.T("Turnovers.LastBalance"), LastBalance?.ToString("N2") ?? "0.00", PdfHeaderBg);
                lastBalanceBlock.Margin = new Thickness(0);
                container.Children.Add(lastBalanceBlock);
            }
            else
            {
                container.Children.Add(table);
            }

            page.Children.Add(container);
            tempPages.Add(page);
            pageNumber++;
            currentY = 0;
        }

        int totalPages = tempPages.Count;
        int finalPageNumber = 1;
        foreach (var finalPage in tempPages)
        {
            AddFooterContent(finalPage, finalPageNumber, totalPages);
            var pageContent = new PageContent();
            ((IAddChild)pageContent).AddChild(finalPage);
            doc.Pages.Add(pageContent);
            finalPageNumber++;
        }
        return doc;
    }

    private void AddOperationRow(Grid grid, CustomerOperationForDisplayViewModel op, double descWidth)
    {
        int row = grid.RowDefinitions.Count;
        double requiredHeight = CalculateOperationRowHeight(op, descWidth);
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(requiredHeight) });

        Brush amountBrush = op.Debit > 0 ? PdfDanger : PdfInk;
        string amountText = op.Debit > 0 ? op.Debit.ToString("N2") : op.Credit.ToString("N2");

        AddSimpleCell(grid, row, 0, op.Date.ToString("dd.MM.yyyy"), TextAlignment.Center, FontWeights.Normal, 12.5, new Thickness(0.5, 0.5, 0, 0.5), PdfInk);

        var borderDesc = new Border
        {
            BorderBrush = PdfLine,
            BorderThickness = new Thickness(0.5, 0, 0, 0.5),
            Padding = new Thickness(6, 4, 6, 4),
            Child = BuildDescriptionContent(op, descWidth - 12)
        };
        Grid.SetRow(borderDesc, row); Grid.SetColumn(borderDesc, 1); grid.Children.Add(borderDesc);

        AddSimpleCell(grid, row, 2, amountText, TextAlignment.Right, FontWeights.Bold, 12.5, new Thickness(0.5, 0, 0.5, 0.5), amountBrush);
    }

    private FrameworkElement BuildDescriptionContent(CustomerOperationForDisplayViewModel op, double width)
    {
        var panel = new StackPanel { Width = width };
        var lines = (op.Description ?? op.FormattedDescription ?? string.Empty).Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        Grid? items = null;
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                items = null;
                continue;
            }

            if (line.All(c => c == '-'))
            {
                items = null;
                panel.Children.Add(BuildSeparator());
                continue;
            }

            if (TryParseItem(line, out var name, out var length, out var price, out var sum, out var disc))
            {
                items ??= AppendItemsGrid(panel);
                AddItemRow(items, name, length, price, sum, disc);
            }
            else
            {
                items = null;
                var t = line.TrimStart();
                if (t.StartsWith("Jami", StringComparison.OrdinalIgnoreCase) || t.StartsWith("Chegirma", StringComparison.OrdinalIgnoreCase))
                    panel.Children.Add(BuildTotalLine(line));
                else
                    panel.Children.Add(BuildTextLine(line));
            }
        }
        return panel;
    }

    private static Grid AppendItemsGrid(StackPanel panel)
    {
        var g = new Grid { Margin = new Thickness(0, 2, 0, 3) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panel.Children.Add(g);
        return g;
    }

    private void AddItemRow(Grid g, string name, string length, string price, string sum, string disc)
    {
        int r = g.RowDefinitions.Count;
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        bool hasDisc = disc.Length > 0;

        g.Children.Add(ItemCell(name, 0, r, TextAlignment.Left, PdfInk, FontWeights.SemiBold, new Thickness(0, 1, 10, 1), true));
        g.Children.Add(ItemCell(length, 1, r, TextAlignment.Right, PdfMuted, FontWeights.Normal, new Thickness(0, 1, 10, 1), false));
        g.Children.Add(ItemCell("× " + price, 2, r, TextAlignment.Right, PdfMuted, FontWeights.Normal, new Thickness(0, 1, 10, 1), false));
        g.Children.Add(ItemCell("= " + sum, 3, r, TextAlignment.Right, PdfInk, FontWeights.Bold, new Thickness(0, 1, hasDisc ? 10 : 0, 1), false));
        if (hasDisc)
            g.Children.Add(ItemCell("(" + disc + ")", 4, r, TextAlignment.Right, PdfDanger, FontWeights.Normal, new Thickness(0, 1, 0, 1), false));
    }

    private static TextBlock ItemCell(string text, int col, int row, TextAlignment align, Brush fg, FontWeight weight, Thickness margin, bool wrap)
    {
        var tb = new TextBlock
        {
            Text = text,
            TextAlignment = align,
            Foreground = fg,
            FontWeight = weight,
            FontSize = 12.5,
            Margin = margin,
            TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap
        };
        Grid.SetColumn(tb, col);
        Grid.SetRow(tb, row);
        return tb;
    }

    private TextBlock BuildTextLine(string line)
    {
        var tb = new TextBlock { FontSize = 12.5, TextWrapping = TextWrapping.Wrap, Foreground = PdfInk, Margin = new Thickness(0, 1, 0, 1) };
        int c = line.IndexOf(':');
        if (c >= 0)
        {
            tb.Inlines.Add(new Run(line[..(c + 1)]) { FontWeight = FontWeights.Bold });
            tb.Inlines.Add(new Run(line[(c + 1)..]));
        }
        else
            tb.Inlines.Add(new Run(line) { FontWeight = FontWeights.Bold });
        return tb;
    }

    private static Border BuildSeparator() => new()
    {
        Height = 0.7,
        Background = new SolidColorBrush(Color.FromArgb(0x66, 0x9C, 0xA3, 0xAF)),
        Margin = new Thickness(40, 5, 40, 5)
    };

    private FrameworkElement BuildTotalLine(string line)
    {
        int c = line.IndexOf(':');
        if (c < 0) return BuildTextLine(line);

        bool isTotal = line.TrimStart().StartsWith("Jami", StringComparison.OrdinalIgnoreCase);
        const double size = 13.5;
        double valueSize = isTotal ? size : 14.5;

        var value = line[(c + 1)..].Trim();
        int p = value.LastIndexOf('(');
        if (p > 0 && value.EndsWith(")"))
            value = $"{value[p..]} {value[..p].Trim()}";

        var g = new Grid { Margin = new Thickness(0, 1, 0, 1) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var lbl = new TextBlock
        {
            Text = line[..c].Trim(),
            FontSize = size,
            FontWeight = FontWeights.Bold,
            Foreground = PdfInk,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var val = new TextBlock
        {
            Text = value,
            FontSize = valueSize,
            FontWeight = isTotal ? FontWeights.Bold : FontWeights.Normal,
            Foreground = PdfInk,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(val, 1);
        g.Children.Add(lbl);
        g.Children.Add(val);
        return g;
    }

    private static string NormalizeDiscount(string disc)
    {
        int pct = disc.IndexOf('%');
        if (pct > 0 && decimal.TryParse(disc[..pct].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
            return $"{rate:0.##}{disc[pct..]}";
        return disc;
    }

    private static bool TryParseItem(string line, out string name, out string length, out string price, out string sum, out string disc)
    {
        name = length = price = sum = disc = string.Empty;
        var body = line.TrimEnd(';', ' ');

        int br = body.IndexOf("[ch:", StringComparison.OrdinalIgnoreCase);
        if (br >= 0)
        {
            int end = body.IndexOf(']', br);
            disc = (end > br ? body[(br + 4)..end] : body[(br + 4)..]).Trim();
            disc = NormalizeDiscount(disc);
            body = body[..br].Trim();
        }

        int eq = body.LastIndexOf('=');
        if (eq < 0)
            return false;
        sum = body[(eq + 1)..].Trim();

        var left = body[..eq].Trim();
        int x = left.LastIndexOf(" x ", StringComparison.Ordinal);
        if (x < 0)
            return false;
        price = left[(x + 3)..].Trim();

        var nameLen = left[..x].Trim();
        int dash = nameLen.LastIndexOf(" - ", StringComparison.Ordinal);
        if (dash < 0)
            return false;
        length = nameLen[(dash + 3)..].Trim();
        name = nameLen[..dash].Trim();
        return name.Length > 0;
    }

    private void AddRowHeader(Grid grid, string date, string description, double height)
    {
        int row = grid.RowDefinitions.Count;
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(height) });

        grid.Children.Add(HeaderCell(date, TextAlignment.Center, new Thickness(0.5, 0.5, 0, 1.2), row, 0));
        grid.Children.Add(HeaderCell(description, TextAlignment.Center, new Thickness(0.5, 0.5, 0, 1.2), row, 1));

        var tb = new TextBlock
        {
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        tb.Inlines.Add(new Run(TranslationSource.T("Turnovers.Debit")) { Foreground = PdfDanger });
        tb.Inlines.Add(new Run(" / ") { Foreground = PdfMuted });
        tb.Inlines.Add(new Run(TranslationSource.T("Turnovers.Credit")) { Foreground = PdfInk });

        var border = new Border
        {
            Background = PdfHeaderBg,
            BorderBrush = PdfAccent,
            BorderThickness = new Thickness(0.5, 0.5, 0.5, 1.2),
            Child = tb
        };

        Grid.SetRow(border, row);
        Grid.SetColumn(border, 2);
        grid.Children.Add(border);
    }

    private Border HeaderCell(string text, TextAlignment align, Thickness borderThickness, int row, int column)
    {
        var border = new Border
        {
            Background = PdfHeaderBg,
            BorderBrush = PdfAccent,
            BorderThickness = borderThickness,
            Child = new TextBlock
            {
                Text = text,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                TextAlignment = align,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = PdfAccent
            }
        };
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        return border;
    }

    private void AddRowTotalNew(Grid grid, decimal totalCredit, decimal totalDebit, double height)
    {
        int row1 = grid.RowDefinitions.Count;
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Children.Add(TotalLine(TranslationSource.T("Turnovers.Debit"), totalDebit.ToString("N2"), PdfDanger, row1, new Thickness(0, 1, 0, 0.6)));

        int row2 = grid.RowDefinitions.Count;
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Children.Add(TotalLine(TranslationSource.T("Turnovers.Credit"), totalCredit.ToString("N2"), PdfInk, row2, new Thickness(0, 0, 0, 1)));
    }

    private Border TotalLine(string label, string value, Brush valueBrush, int row, Thickness borderThickness)
    {
        var inner = new Grid();
        inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var lbl = new TextBlock
        {
            Text = label,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = PdfInk,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(2, 6, 10, 6)
        };
        var val = new TextBlock
        {
            Text = value,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = valueBrush,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(10, 6, 2, 6)
        };
        Grid.SetColumn(val, 1);
        inner.Children.Add(lbl);
        inner.Children.Add(val);

        var border = new Border
        {
            BorderBrush = PdfMuted,
            BorderThickness = borderThickness,
            Child = inner
        };
        Grid.SetRow(border, row);
        Grid.SetColumnSpan(border, 3);
        return border;
    }

    private void AddSimpleCell(Grid grid, int row, int column, string value, TextAlignment align, FontWeight weight, double size, Thickness borderThickness, Brush foreground)
    {
        var tb = new TextBlock
        {
            Text = value,
            Padding = new Thickness(5, 2, 5, 2),
            FontSize = size,
            FontWeight = weight,
            TextAlignment = align,
            Foreground = foreground,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var border = new Border
        {
            BorderBrush = PdfLine,
            BorderThickness = borderThickness,
            Child = tb
        };

        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        grid.Children.Add(border);
    }

    private Border CreateBalanceInfoBlock(string label, string value, Brush background)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lblText = new TextBlock
        {
            Text = label,
            FontSize = 13.5,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(10, 6, 10, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = PdfMuted
        };

        var valText = new TextBlock
        {
            Text = value,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(10, 6, 10, 6),
            HorizontalAlignment = HorizontalAlignment.Right,
            Foreground = PdfInk
        };

        Grid.SetColumn(lblText, 0);
        Grid.SetColumn(valText, 1);
        grid.Children.Add(lblText);
        grid.Children.Add(valText);

        return new Border
        {
            Background = background,
            BorderBrush = PdfAccent,
            BorderThickness = new Thickness(0, 0, 0, 1.2),
            Child = grid
        };
    }

    private void AddFooterContent(FixedPage page, int currentPage, int totalPages)
    {
        const double margin = 40;

        var pageInfo = new TextBlock
        {
            Text = string.Format(TranslationSource.T("Turnovers.PageOfPages"), currentPage, totalPages),
            FontSize = 10,
            Foreground = PdfMuted,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        FixedPage.SetRight(pageInfo, margin);
        FixedPage.SetBottom(pageInfo, 20);

        page.Children.Add(pageInfo);
    }

    private double AddHeaderContent(StackPanel container, int pageNumber, bool isFullHeader)
    {
        if (isFullHeader)
        {
            var head = new Grid();
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(new TextBlock { Text = ReportService.Brand, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = PdfAccent });
            left.Children.Add(new TextBlock { Text = TranslationSource.T("Turnovers.OperationsReportName"), FontSize = 19, FontWeight = FontWeights.Bold, Foreground = PdfInk });
            left.Children.Add(new TextBlock
            {
                Text = $"{SelectedCustomer?.Name}  |  {BeginDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}",
                FontSize = 11,
                Foreground = PdfMuted,
                Margin = new Thickness(0, 3, 0, 0)
            });
            Grid.SetColumn(left, 0);
            head.Children.Add(left);

            var date = new TextBlock { Text = $"{DateTime.Now:dd.MM.yyyy  HH:mm}", FontSize = 10, Foreground = PdfMuted, VerticalAlignment = VerticalAlignment.Bottom, TextAlignment = TextAlignment.Right };
            Grid.SetColumn(date, 1);
            head.Children.Add(date);

            container.Children.Add(head);
            container.Children.Add(new Border { Height = 2.5, Background = PdfAccent, Margin = new Thickness(0, 8, 0, 12) });
            return 120;
        }
        else
        {
            container.Children.Add(new TextBlock
            {
                Text = string.Empty,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 10)
            });
            return 30;
        }
    }

    private double CalculateOperationRowHeight(CustomerOperationForDisplayViewModel op, double descWidth)
    {
        var content = BuildDescriptionContent(op, descWidth - 12);
        content.Measure(new Size(descWidth - 12, double.MaxValue));
        return Math.Max(28, content.DesiredSize.Height + 12);
    }

    #endregion PDF Export and Share
}
