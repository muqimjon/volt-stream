namespace VoltStream.WPF.Debitors.Models;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using PdfSharp.Drawing;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.ViewModels;

public partial class DebitorCreditorPageViewModel : ViewModelBase
{
    private readonly IServiceProvider service;
    private readonly ICustomersApi customersApi;
    private readonly IMapper mapper;

    public DebitorCreditorPageViewModel(IServiceProvider service)
    {
        this.service = service;
        customersApi = service.GetRequiredService<ICustomersApi>();
        _ = LoadDate();
        mapper = service.GetRequiredService<IMapper>();
    }

    [ObservableProperty] private ObservableCollection<CustomerViewModel> availableCustomers = [];
    [ObservableProperty] private ObservableCollection<DebitorCreditorItemViewModel> debitorCreditorItems = [];

    [ObservableProperty]
    private ObservableCollection<DebitorCreditorItemViewModel> filteredDebitorCreditorItems = [];
    [ObservableProperty] private CustomerViewModel? selectedCustomer;
    [ObservableProperty] private decimal finalDebitor;
    [ObservableProperty] private decimal finalKreditor;
    [ObservableProperty] private decimal finalAmount;
    [ObservableProperty] private decimal finalDiscount;


    [ObservableProperty] private string? sign;
    [ObservableProperty] private decimal amount;

    public List<string> Signs { get; } = [">", ">=", "=", "<", "<=", "<>"];

    partial void OnDebitorCreditorItemsChanged(ObservableCollection<DebitorCreditorItemViewModel> value)
    {
        FilteredDebitorCreditorItems = new ObservableCollection<DebitorCreditorItemViewModel>(value);
        FinalAmount = value.Sum(x => x.TotalBalance);
    }

    [ObservableProperty] private decimal totalBalance;


    partial void OnSignChanged(string? value) => ApplyFilter();
    partial void OnAmountChanged(decimal value) => ApplyFilter();

    partial void OnSelectedCustomerChanged(CustomerViewModel? value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (DebitorCreditorItems == null) return;

        var filtered = DebitorCreditorItems.ToList();

        if (SelectedCustomer != null)
            filtered = [.. filtered.Where(x => x.Customer == SelectedCustomer.Name)];

        if (!string.IsNullOrEmpty(Sign) && Amount > 0)
        {
            filtered = Sign switch
            {
                ">" => [.. filtered.Where(x => (x.Debitor > 0 && x.Debitor > Amount) ||
                                            (x.Creditor > 0 && x.Creditor > Amount))],
                ">=" => [.. filtered.Where(x => (x.Debitor > 0 && x.Debitor >= Amount) ||
                                            (x.Creditor > 0 && x.Creditor >= Amount))],
                "=" => [.. filtered.Where(x => x.Debitor == Amount || x.Creditor == Amount)],
                "<" => [.. filtered.Where(x => (x.Debitor > 0 && x.Debitor < Amount) ||
                                            (x.Creditor > 0 && x.Creditor < Amount))],
                "<=" => [.. filtered.Where(x => (x.Debitor > 0 && x.Debitor <= Amount) ||
                                            (x.Creditor > 0 && x.Creditor <= Amount))],
                "<>" => [.. filtered.Where(x => x.Debitor != Amount && x.Creditor != Amount)],
                _ => filtered
            };
        }

        FilteredDebitorCreditorItems = new ObservableCollection<DebitorCreditorItemViewModel>(filtered);

        FinalDebitor = filtered.Sum(x => x.Debitor);
        FinalKreditor = filtered.Sum(x => x.Creditor);
        FinalDiscount = filtered.Sum(x => x.Discount);

        FinalAmount = FinalDebitor - FinalKreditor - FinalDiscount;
    }
    private async Task LoadDate()
    {
        await LoadCustomers();
    }

    private async Task LoadCustomers()
    {
        var response = await customersApi.GetAllAsync().Handle(isLoading => IsLoading = isLoading);
        if (!response.IsSuccess)
        {
            Error = response.Message ?? "Mijozlarni yuklashda xatolik!";
            return;
        }

        AvailableCustomers = mapper.Map<ObservableCollection<CustomerViewModel>>(response.Data);

        var items = response.Data.Select(c =>
        {
            var discount = c.Accounts!.First().Discount;
            var totalBalance = c.Accounts!.Sum(a => a.Balance);

            return new DebitorCreditorItemViewModel
            {
                Customer = c.Name,
                Phone = c.Phone!,
                Address = c.Address!,
                Discount = discount,
                Debitor = totalBalance < 0 ? -totalBalance : 0,
                Creditor = totalBalance > 0 ? totalBalance : 0,
                TotalBalance = totalBalance
            };
        }).ToList();

        DebitorCreditorItems = new ObservableCollection<DebitorCreditorItemViewModel>(items);
        FilteredDebitorCreditorItems = new ObservableCollection<DebitorCreditorItemViewModel>(items);

        FinalDebitor = items.Sum(x => x.Debitor);
        FinalKreditor = items.Sum(x => x.Creditor);
        FinalDiscount = items.Sum(x => x.Discount);

        FinalAmount = FinalDebitor - FinalKreditor - FinalDiscount;
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SelectedCustomer = null;
        Sign = null;
        Amount = 0;
        ApplyFilter();
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        try
        {
            if (FilteredDebitorCreditorItems == null || !FilteredDebitorCreditorItems.Any())
            {
                Info = "Eksport uchun ma'lumot yo'q.";
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel fayllari (*.xlsx)|*.xlsx",
                FileName = "Debitor va Kreditorlar.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            var sumDiscount = FilteredDebitorCreditorItems.Sum(x => x.Discount);
            var sumDebitor = FilteredDebitorCreditorItems.Sum(x => x.Debitor);
            var sumCreditor = FilteredDebitorCreditorItems.Sum(x => x.Creditor);
            var umumiyBalans = sumDebitor - sumDiscount - sumCreditor;

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("DebitorKreditor");

                ws.Cell(1, 1).Value = "DEBITOR VA KREDITORLAR HISOBOTI";
                ws.Range("A1:F1").Merge();
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 16;
                ws.Cell(1, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                string[] headers = new[] { "Mijoz", "Telefon", "Manzil", "Bonus", "Debitor", "Kreditor" };
                for (int i = 0; i < headers.Length; i++)
                    ws.Cell(3, i + 1).Value = headers[i];

                var headerRange = ws.Range("A3:F3");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                int row = 4;
                foreach (var item in FilteredDebitorCreditorItems)
                {
                    ws.Cell(row, 1).Value = item.Customer;
                    ws.Cell(row, 2).Value = item.Phone;
                    ws.Cell(row, 3).Value = item.Address;
                    ws.Cell(row, 4).Value = item.Discount;
                    ws.Cell(row, 5).Value = item.Debitor;
                    ws.Cell(row, 6).Value = item.Creditor;

                    for (int col = 4; col <= 6; col++)
                        ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";

                    row++;
                }

                int jamiRow = row;
                ws.Cell(jamiRow, 1).Value = "Jami:";
                ws.Range(jamiRow, 1, jamiRow, 3).Merge();
                ws.Cell(jamiRow, 1).Style.Font.Bold = true;
                ws.Cell(jamiRow, 4).Value = sumDiscount;
                ws.Cell(jamiRow, 5).Value = sumDebitor;
                ws.Cell(jamiRow, 6).Value = sumCreditor;
                ws.Range(jamiRow, 4, jamiRow, 6).Style.Font.Bold = true;
                ws.Range(jamiRow, 4, jamiRow, 6).Style.NumberFormat.Format = "#,##0.00";

                int umumiyRow = jamiRow + 1;
                ws.Cell(umumiyRow, 1).Value = "Umumiy balans:";
                ws.Cell(umumiyRow, 1).Style.Font.Bold = true;
                ws.Range(umumiyRow, 1, umumiyRow, 5).Merge();
                ws.Cell(umumiyRow, 6).Value = umumiyBalans;
                ws.Cell(umumiyRow, 6).Style.Font.Bold = true;
                ws.Cell(umumiyRow, 6).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(umumiyRow, 6).Style.Font.FontColor = umumiyBalans > 0
                    ? ClosedXML.Excel.XLColor.Green
                    : ClosedXML.Excel.XLColor.Red;

                ws.Columns().AdjustToContents();
                workbook.SaveAs(dialog.FileName);
            }

            Success = "Ma'lumotlar muvaffaqiyatli eksport qilindi";
        }
        catch (Exception ex) { Error = $"Excel faylga eksport qilishda xatolik yuz berdi: {ex.Message}"; }
    }

    [RelayCommand]
    private void Print()
    {
        if (FilteredDebitorCreditorItems == null || !FilteredDebitorCreditorItems.Any())
        {
            Info = "Chop etish uchun ma’lumot topilmadi.";
            return;
        }

        var fixedDoc = CreateFixedDocumentForPrint();
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() == true)
            dlg.PrintDocument(fixedDoc.DocumentPaginator, "Debitor va Kreditorlar");
    }

    [RelayCommand]
    private void Preview()
    {
        if (FilteredDebitorCreditorItems == null || !FilteredDebitorCreditorItems.Any())
        {
            Info = "Ko‘rsatish uchun ma’lumot yo‘q.";
            return;
        }

        FinalAmount = FinalDebitor - FinalKreditor - FinalDiscount;
        var fixedDoc = CreateFixedDocumentForPrint();

        var viewer = new DocumentViewer
        {
            Document = fixedDoc,
            Margin = new Thickness(10, 5, 5, 5)
        };

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(5)
        };

        var shareButton = new Button
        {
            Content = "📤 Telegram’da ulashish",
            Margin = new Thickness(5, 0, 0, 0),
            Padding = new Thickness(10, 5, 10, 5)
        };
        shareButton.Click += (s, e) =>
        {
            try
            {
                string pdfPath = Path.Combine(Path.GetTempPath(), $"DebitorKreditor_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                SaveFixedDocumentToPdf(fixedDoc, pdfPath);

                if (!File.Exists(pdfPath))
                {
                    Error = "PDF fayl yaratilmagan.";
                    return;
                }

                SharePdfFile(pdfPath);
            }
            catch (Exception ex) { Error = $"Faylni ulashishda xatolik yuz berdi: {ex.Message}"; }
        };

        toolbar.Children.Add(shareButton);

        var layout = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        layout.Children.Add(toolbar);
        layout.Children.Add(viewer);

        var previewWindow = new Window
        {
            Title = "Debitor/Kreditor Preview",
            Width = 900,
            Height = 800,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = layout
        };

        previewWindow.ShowDialog();
    }

    private void SaveFixedDocumentToPdf(FixedDocument fixedDoc, string pdfPath)
    {
        try
        {
            using var document = new PdfSharp.Pdf.PdfDocument();

            foreach (var pageContent in fixedDoc.Pages)
            {
                var fixedPage = pageContent.GetPageRoot(false);
                if (fixedPage == null)
                    continue;

                fixedPage.Measure(new Size(fixedPage.Width, fixedPage.Height));
                fixedPage.Arrange(new Rect(new Size(fixedPage.Width, fixedPage.Height)));
                fixedPage.UpdateLayout();

                var bmp = new RenderTargetBitmap(
                    (int)fixedPage.Width,
                    (int)fixedPage.Height,
                    96, 96,
                    PixelFormats.Pbgra32);
                bmp.Render(fixedPage);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                using var ms = new MemoryStream();
                encoder.Save(ms);
                ms.Position = 0;

                var pdfPage = document.AddPage();
                pdfPage.Width = XUnit.FromPoint(fixedPage.Width);
                pdfPage.Height = XUnit.FromPoint(fixedPage.Height);

                using var gfx = XGraphics.FromPdfPage(pdfPage);
                using var image = XImage.FromStream(ms);
                gfx.DrawImage(image, 0, 0, pdfPage.Width, pdfPage.Height);
            }

            document.Save(pdfPath);
        }
        catch (Exception ex) { Error = $"PDF faylni saqlashda xatolik yuz berdi: {ex.Message}"; }
    }

    private void SharePdfFile(string pdfPath)
    {
        try
        {
            if (!File.Exists(pdfPath))
            {
                Error = "Fayl topilmadi.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{pdfPath}\"",
                UseShellExecute = true
            });

        }
        catch (Exception ex) { Error = $"Ulashishda xatolik yuz berdi: {ex.Message}"; }
    }

    private FixedDocument CreateFixedDocumentForPrint()
    {
        double pageWidth = 793.7;
        double pageHeight = 1122.5;
        double margin = 40;

        var fixedDoc = new FixedDocument();
        fixedDoc.DocumentPaginator.PageSize = new Size(pageWidth, pageHeight);

        // A4 balandligiga (margin + sarlavha + "Jami/Umumiy" qatorlari bilan birga) sig'adigan
        // qatorlar soni. Avval 50 edi - bu sahifadan oshib ketib, pastki qatorlar kesilib qolardi.
        const int maxRowsPerPage = 33;
        int pageNumber = 0;
        var items = FilteredDebitorCreditorItems.ToList();
        int totalPages = (int)Math.Ceiling(items.Count / (double)maxRowsPerPage);
        int processedItems = 0;

        double availableWidth = pageWidth - (2 * margin);

        double col1 = availableWidth * 0.20; // Mijoz
        double col2 = availableWidth * 0.12; // Telefon
        double col3 = availableWidth * 0.20; // Manzil
        double col4 = availableWidth * 0.15; // Bonus
        double col5 = availableWidth * 0.16; // Debitor
        double col6 = availableWidth * 0.17; // Kreditor

        var sumDiscount = items.Sum(x => x.Discount);
        var sumDebitor = items.Sum(x => x.Debitor);
        var sumCreditor = items.Sum(x => x.Creditor);
        var umumiyBalans = sumDebitor - sumDiscount - sumCreditor;

        while (processedItems < items.Count)
        {
            pageNumber++;
            var page = new FixedPage { Width = pageWidth, Height = pageHeight, Background = Brushes.White };
            var grid = new Grid { Margin = new Thickness(margin, 70, margin, 70) };

            double[] widths = new[] { col1, col2, col3, col4, col5, col6 };
            foreach (var w in widths)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(w) });

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            int row = 0;

            string[] headers = new[] { "Mijoz", "Telefon", "Manzil", "Bonus", "Debitor", "Kreditor" };
            for (int i = 0; i < headers.Length; i++)
            {
                var border = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    Background = Brushes.LightGray,
                    Padding = new Thickness(6)
                };
                var text = new TextBlock
                {
                    Text = headers[i],
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                border.Child = text;
                Grid.SetRow(border, row);
                Grid.SetColumn(border, i);
                grid.Children.Add(border);
            }

            var pageItems = items.Skip(processedItems).Take(maxRowsPerPage).ToList();
            foreach (var item in pageItems)
            {
                row++;
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                string[] values = new[]
                {
                item.Customer ?? string.Empty,
                item.Phone ?? string.Empty,
                item.Address ?? string.Empty,
                item.Discount.ToString("N2"),
                (item.Debitor > 0 ? item.Debitor.ToString("N2") : ""),
                (item.Creditor > 0 ? item.Creditor.ToString("N2") : "")
            };

                for (int i = 0; i < values.Length; i++)
                {
                    var border = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0.5),
                        Padding = new Thickness(5)
                    };
                    var text = new TextBlock
                    {
                        Text = values[i],
                        FontSize = 13,
                        TextAlignment = (i >= 3 ? TextAlignment.Right : TextAlignment.Left),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    border.Child = text;
                    Grid.SetRow(border, row);
                    Grid.SetColumn(border, i);
                    grid.Children.Add(border);
                }
            }

            processedItems += pageItems.Count;

            if (processedItems >= items.Count)
            {
                row++;
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
                row++;
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var jamiBorder = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0.5),
                    Padding = new Thickness(5),
                    Child = new TextBlock
                    {
                        Text = "Jami:",
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                Grid.SetRow(jamiBorder, row);
                Grid.SetColumn(jamiBorder, 0);
                Grid.SetColumnSpan(jamiBorder, 3);
                grid.Children.Add(jamiBorder);

                AddTotalCell(grid, row, 3, sumDiscount);
                AddTotalCell(grid, row, 4, sumDebitor);
                AddTotalCell(grid, row, 5, sumCreditor);

                row++;
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var umumiyBorder = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0.5),
                    Padding = new Thickness(5),
                    Child = new TextBlock
                    {
                        Text = "Umumiy balans:",
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                Grid.SetRow(umumiyBorder, row);
                Grid.SetColumn(umumiyBorder, 0);
                Grid.SetColumnSpan(umumiyBorder, 5);
                grid.Children.Add(umumiyBorder);

                AddTotalCell(grid, row, 5, umumiyBalans, true);
            }

            var title = new TextBlock
            {
                Text = "DEBITOR VA KREDITORLAR HISOBOTI",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 15, 0, 15)
            };
            FixedPage.SetTop(title, 15);
            FixedPage.SetLeft(title, 50);
            page.Children.Add(title);

            var pageNum = new TextBlock
            {
                Text = $"Sahifa {pageNumber} / {totalPages}",
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 60, 20)
            };
            FixedPage.SetBottom(pageNum, 20);
            FixedPage.SetRight(pageNum, 50);
            page.Children.Add(pageNum);

            page.Children.Add(grid);

            var pageContent = new PageContent();
            ((IAddChild)pageContent).AddChild(page);
            fixedDoc.Pages.Add(pageContent);
        }

        return fixedDoc;
    }

    private static void AddTotalCell(Grid grid, int row, int col, decimal value, bool isFinal = false)
    {
        var cell = new Border
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(0.5),
            Background = isFinal ? Brushes.AliceBlue : Brushes.White,
            Padding = new Thickness(5)
        };

        var text = new TextBlock
        {
            Text = value.ToString("N2"),
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            TextAlignment = TextAlignment.Right,
            Foreground = isFinal
                ? (value > 0 ? Brushes.Green : Brushes.Red)
                : Brushes.Black,
            VerticalAlignment = VerticalAlignment.Center
        };

        cell.Child = text;
        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, col);
        grid.Children.Add(cell);
    }
}
