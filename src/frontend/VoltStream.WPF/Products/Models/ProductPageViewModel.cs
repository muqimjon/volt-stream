namespace VoltStream.WPF.Products.Models;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using ApiServices.Models.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using VoltStream.WPF.Commons;

public partial class ProductPageViewModel : ViewModelBase
{
    private readonly IServiceProvider services;
    private readonly IMapper mapper;

    public ProductPageViewModel(IServiceProvider services)
    {
        mapper = services.GetRequiredService<IMapper>();
        this.services = services;
        _ = LoadInitialDataAsync();
    }

    [ObservableProperty] private CategoryResponse? selectedCategory;
    [ObservableProperty] private ObservableCollection<CategoryResponse> categories = [];

    [ObservableProperty] private ProductResponse? selectedProduct;
    [ObservableProperty] private ObservableCollection<ProductResponse> allProducts = [];
    [ObservableProperty] private ObservableCollection<ProductResponse> products = [];

    [ObservableProperty] private ObservableCollection<ProductItemViewModel> productItems = [];
    [ObservableProperty] private ObservableCollection<ProductItemViewModel> filteredProductItems = [];

    [ObservableProperty] private decimal? finalAmount;

    // Belgilanganda 0 qoldiqli mahsulotlar ham ko'rsatiladi (aks holda faqat qoldig'i bor mahsulotlar).
    [ObservableProperty] private bool showAllBalances;

    partial void OnShowAllBalancesChanged(bool value) => ApplyFilter();

    private async Task LoadInitialDataAsync()
    {
        await Task.WhenAll(
            LoadCategoriesAsync(),
            LoadProductsAsync(),
            LoadWarehouseItemsAsync());

        ApplyFilter();
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SelectedCategory = null;
        SelectedProduct = null;
        ShowAllBalances = false;

        Products = new ObservableCollection<ProductResponse>(AllProducts);

        ApplyFilter();
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        try
        {
            if (FilteredProductItems == null || !FilteredProductItems.Any())
            {
                Error = "Eksport qilish uchun ma'lumot topilmadi.";
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel fayllari (*.xlsx)|*.xlsx",
                FileName = "Mahsulotlar.xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Mahsulotlar");

                worksheet.Cell(1, 1).Value = "Mahsulot turi";
                worksheet.Cell(1, 2).Value = "Nomi";
                worksheet.Cell(1, 3).Value = "Rulon uzunligi";
                worksheet.Cell(1, 4).Value = "Rulon soni";
                worksheet.Cell(1, 5).Value = "Jami";
                worksheet.Cell(1, 6).Value = "O‘lchov birligi";
                worksheet.Cell(1, 7).Value = "Narxi";
                worksheet.Cell(1, 8).Value = "Umumiy summa";

                var headerRange = worksheet.Range("A1:H1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var item in FilteredProductItems)
                {
                    worksheet.Cell(row, 1).Value = item.Category;
                    worksheet.Cell(row, 2).Value = item.Name;

                    worksheet.Cell(row, 3).Value = item.RollLength;
                    worksheet.Cell(row, 3).Value = (int)item.RollLength!;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";

                    worksheet.Cell(row, 4).Value = item.Quantity;
                    worksheet.Cell(row, 4).Value = (int)item.Quantity!;
                    worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";

                    worksheet.Cell(row, 5).Value = item.TotalCount;
                    worksheet.Cell(row, 5).Value = (int)item.TotalCount!;
                    worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

                    worksheet.Cell(row, 6).Value = item.Unit;

                    worksheet.Cell(row, 7).Value = item.Price;
                    worksheet.Cell(row, 7).Value = (decimal)item.Price!;
                    worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

                    worksheet.Cell(row, 8).Value = item.TotalAmount;
                    worksheet.Cell(row, 8).Value = (decimal)item.TotalAmount!;
                    worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";

                    worksheet.Range(row, 3, row, 8)
                        .Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;

                    worksheet.Cell(row, 6).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    row++;
                }

                worksheet.Cell(row, 1).Value = "Jami";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 8).Value = FinalAmount ?? 0;
                worksheet.Cell(row, 8).Style.Font.Bold = true;
                worksheet.Cell(row, 8).Value = (decimal)FinalAmount!;
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 8).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;

                worksheet.Columns().AdjustToContents();

                double headerWidth = worksheet.Cell(1, 8).GetValue<string>().Length + 5;
                worksheet.Column(8).Width = headerWidth;

                workbook.SaveAs(dialog.FileName);
            }

            Success = "Ma'lumotlar muvaffaqiyatli Excel faylga eksport qilindi ✅";
        }
        catch (Exception ex) { Error = $"Xatolik yuz berdi: {ex.Message}"; }
    }


    [RelayCommand]
    private void Print()
    {
        if (FilteredProductItems == null || !FilteredProductItems.Any())
        {
            Info = "Chop etish uchun ma’lumot topilmadi.";
            return;
        }

        var fixedDoc = CreateFixedDocument();

        var dlg = new PrintDialog();
        if (dlg.ShowDialog() == true)
        {
            dlg.PrintDocument(fixedDoc.DocumentPaginator, "Mahsulotlar ro'yxati");
        }
    }

    [RelayCommand]
    private void Preview()
    {
        if (FilteredProductItems == null || !FilteredProductItems.Any())
        {
            Info = "Ko‘rsatish uchun ma’lumot yo‘q!";
            return;
        }

        var doc = CreateFixedDocument();
        var viewer = new DocumentViewer { Document = doc, Margin = new Thickness(10) };
        var window = new Window
        {
            Title = "Ombor qoldiqlari hisoboti",
            Width = 1000,
            Height = 800,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = viewer
        };
        window.ShowDialog();
    }

    private FixedDocument CreateFixedDocument()
    {
        var doc = new FixedDocument();
        const double pageWidth = 793.7;
        const double pageHeight = 1122.5;
        const double margin = 30;
        const double bottomReservedSpace = 70;

        var itemsList = FilteredProductItems.ToList();

        decimal totalSumma = itemsList.Sum(item =>
        {
            _ = decimal.TryParse(item.TotalAmount?.ToString(), out decimal val);
            return val;
        });

        int currentItemIndex = 0;
        int pageNumber = 1;
        double[] widths = [35, 90, 100, 70, 80, 75, 60, 80, 120];

        while (currentItemIndex < itemsList.Count)
        {
            var page = new FixedPage { Width = pageWidth, Height = pageHeight, Background = Brushes.White };
            double currentTop = 30;

            if (pageNumber == 1)
            {
                var header = CreateHeader(pageWidth, margin);
                FixedPage.SetTop(header, currentTop);
                FixedPage.SetLeft(header, margin);
                page.Children.Add(header);
                currentTop = 100;
            }
            else
            {
                currentTop = 40;
            }

            var grid = new Grid { Width = pageWidth - (margin * 2) };
            foreach (var w in widths) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(w) });

            AddRow(grid, true, "№", "Mahsulot turi", "Nomi", "To'plamda", "To'plam soni", "Jami", "Birligi", "Narxi", "Summasi");

            while (currentItemIndex < itemsList.Count)
            {
                var item = itemsList[currentItemIndex];

                string rollLen = double.TryParse(item.RollLength?.ToString(), out double rl) ? rl.ToString("N0") : "0";
                string qty = double.TryParse(item.Quantity?.ToString(), out double q) ? q.ToString("N0") : "0";
                string totalCnt = double.TryParse(item.TotalCount?.ToString(), out double tc) ? tc.ToString("N0") : "0";
                string price = double.TryParse(item.Price?.ToString(), out double p) ? p.ToString("N2") : "0.00";
                string amount = double.TryParse(item.TotalAmount?.ToString(), out double ta) ? ta.ToString("N2") : "0.00";

                string[] rowData = {
        (currentItemIndex + 1).ToString(),
        item.Category ?? "-",
        item.Name ?? "-",
        rollLen,
        qty,
        totalCnt,
        item.Unit ?? "-",
        price,
        amount
    };

                double rowHeight = EstimateRowHeight(grid.Width, widths, rowData, false);

                if (currentTop + grid.DesiredSize.Height + rowHeight > pageHeight - bottomReservedSpace)
                    break;

                AddRow(grid, false, rowData);
                grid.Measure(new Size(grid.Width, double.PositiveInfinity));
                currentItemIndex++;
            }

            if (currentItemIndex == itemsList.Count)
            {
                string[] footerRow = { "", "JAMI:", "", "", "", "", "", "", totalSumma.ToString("N2") };

                double footerHeight = 35;
                if (currentTop + grid.DesiredSize.Height + footerHeight < pageHeight - bottomReservedSpace)
                {
                    AddRow(grid, true, footerRow);
                }
                else
                {
                    // Agar sig'masa, yangi betga o'tib yozish kerak bo'ladi (kamdan-kam holat)
                    // Bu yerda break qilsangiz, keyingi betga faqat JAMI o'zi chiqadi
                }
            }

            FixedPage.SetTop(grid, currentTop);
            FixedPage.SetLeft(grid, margin);
            page.Children.Add(grid);

            var footer = new TextBlock
            {
                Text = $"{pageNumber}-bet",
                FontSize = 10,
                Width = pageWidth - (margin * 2),
                TextAlignment = TextAlignment.Right
            };
            FixedPage.SetTop(footer, pageHeight - 40);
            FixedPage.SetLeft(footer, margin);
            page.Children.Add(footer);

            var pageContent = new PageContent();
            ((IAddChild)pageContent).AddChild(page);
            doc.Pages.Add(pageContent);
            pageNumber++;
        }

        return doc;
    }

    private double EstimateRowHeight(double width, double[] widths, string[] data, bool isHeader)
    {
        var testGrid = new Grid { Width = width };
        foreach (var w in widths)
            testGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(w) });

        // Vaqtincha qator qo'shamiz
        AddRow(testGrid, isHeader, data);

        // O'lchashni amalga oshiramiz
        testGrid.Measure(new Size(width, double.PositiveInfinity));

        return testGrid.DesiredSize.Height;
    }
    private FrameworkElement CreateHeader(double pageWidth, double margin)
    {
        var panel = new StackPanel { Width = pageWidth - (margin * 2) };
        panel.Children.Add(new TextBlock
        {
            Text = "OMBORXONADAGI MAHSULOTLAR QOLDIQLARI",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 5)
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Sana: {DateTime.Now:dd.MM.yyyy HH:mm}",
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        return panel;
    }

    private void AddRow(Grid grid, bool isHeader, params string[] values)
    {
        int row = grid.RowDefinitions.Count;
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int i = 0; i < values.Length; i++)
        {
            // Matnni tekislash (Alignment)
            TextAlignment alignment = TextAlignment.Center;
            if (!isHeader)
            {
                if (i == 1 || i == 2) alignment = TextAlignment.Left; // Turi va Nomi
                if (i >= 3 && i != 6) alignment = TextAlignment.Right; // Raqamli ustunlar
            }

            var tb = new TextBlock
            {
                Text = values[i],
                Padding = new Thickness(4),
                FontSize = isHeader ? 11 : 10,
                FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
                TextAlignment = alignment,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            var border = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5),
                Background = isHeader ? Brushes.LightGray : Brushes.Transparent,
                Child = tb
            };

            Grid.SetRow(border, row);
            Grid.SetColumn(border, i);
            grid.Children.Add(border);
        }
    }

    public async Task LoadWarehouseItemsAsync()
    {
        FilteringRequest request = new() { Filters = new() { ["Product"] = ["include:Category"] } };

        var srvc = services.GetRequiredService<IWarehouseStocksApi>();
        var response = await srvc.Filter(request).Handle(l => IsLoading = l);
        if (!response.IsSuccess)
        {
            Error = response.Message ?? "Ombor qoldiqlarini yuklashda xatolik yuz berdi.";
            return;
        }

        ProductItems.Clear();
        foreach (var item in response.Data!)
        {
            ProductItems.Add(new ProductItemViewModel
            {
                Category = item.Product.Category.Name,
                Name = item.Product.Name,
                RollLength = item.LengthPerRoll,
                Quantity = item.RollCount,
                Price = item.UnitPrice,
                Unit = item.Product.Unit,
                TotalCount = (int)item.TotalLength,
            });
        }
    }

    partial void OnSelectedCategoryChanged(CategoryResponse? value)
    {
        if (value != null)
        {
            var filteredProducts = AllProducts
                .Where(p => p.CategoryId == value.Id)
                .ToList();

            Products.Clear();
            foreach (var product in filteredProducts)
                Products.Add(product);

            if (SelectedProduct != null && SelectedProduct.CategoryId != value.Id)
                SelectedProduct = null;
        }
        else
        {
            Products.Clear();
            foreach (var product in AllProducts)
                Products.Add(product);
        }

        ApplyFilter();
    }

    partial void OnSelectedProductChanged(ProductResponse? value)
    {
        ApplyFilter();
    }

    public async Task LoadCategoriesAsync()
    {
        var response = await services.GetRequiredService<ICategoriesApi>().GetAllAsync().Handle(l => IsLoading = l);
        if (response.IsSuccess) Categories = mapper.Map<ObservableCollection<CategoryResponse>>(response.Data!);
        else Error = response.Message ?? "Kategoriyalar yuklanmadi.";
    }

    public async Task LoadProductsAsync()
    {
        var response = await services.GetRequiredService<IProductsApi>().GetAllAsync().Handle(l => IsLoading = l);
        if (response.IsSuccess)
        {
            AllProducts = mapper.Map<ObservableCollection<ProductResponse>>(response.Data!);
            Products.Clear();
            foreach (var product in AllProducts)
                Products.Add(product);
        }
        else Error = response.Message ?? "Mahsulotlar yuklanmadi.";
    }

    private void ApplyFilter()
    {
        // ShowAllBalances belgilangan bo'lsa 0 qoldiqlar ham ko'rsatiladi.
        IEnumerable<ProductItemViewModel> filtered = ShowAllBalances
            ? ProductItems
            : ProductItems.Where(pi => pi.TotalCount > 0);

        if (SelectedCategory != null)
            filtered = filtered.Where(x => x.Category == SelectedCategory.Name);

        if (SelectedProduct != null)
            filtered = filtered.Where(x => x.Name == SelectedProduct.Name);

        FilteredProductItems = new ObservableCollection<ProductItemViewModel>(filtered);
        FinalAmount = FilteredProductItems.Sum(x => x.TotalAmount);
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductItemViewModel.TotalAmount))
            RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        FinalAmount = ProductItems.Sum(x => x.TotalAmount);
    }
}