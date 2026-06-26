namespace VoltStream.WPF.Home.Models;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using VoltStream.WPF.Commons;

public partial class DashboardPageViewModel : ViewModelBase
{
    // Chiziqli grafikning mantiqiy o'lchami (Viewbox bilan kartaga moslashadi)
    private const double ChartWidth = 660.0;
    private const double ChartHeight = 150.0;
    private const double PadX = 24.0;   // chetlardan gorizontal bo'shliq (nuqta/yorliq kesilmasligi uchun)
    private const double PadTop = 10.0; // yuqoridan bo'shliq

    private readonly IDashboardApi dashboardApi;
    private readonly IMarketDataApi marketDataApi;

    [ObservableProperty] private decimal todaySalesAmount;
    [ObservableProperty] private int todaySalesCount;
    [ObservableProperty] private decimal todaySalesLength;

    [ObservableProperty] private decimal monthSalesAmount;
    [ObservableProperty] private decimal monthSalesLength;

    [ObservableProperty] private decimal todayPaymentsAmount;
    [ObservableProperty] private int todayPaymentsCount;

    [ObservableProperty] private ObservableCollection<TopCustomerResponse> topCustomers = [];
    [ObservableProperty] private ObservableCollection<TopProductResponse> topSellingProducts = [];

    // Oxirgi 7 kunlik savdo - chiziqli grafik geometriyasi
    [ObservableProperty] private PointCollection weeklyLinePoints = [];
    [ObservableProperty] private PointCollection weeklyAreaPoints = [];
    [ObservableProperty] private ObservableCollection<ChartPoint> weeklyPoints = [];

    // Tashqi bozor ma'lumotlari (CBU valyuta kursi + investing.com metall narxlari)
    [ObservableProperty] private bool hasRates;
    [ObservableProperty] private decimal usdRate;
    [ObservableProperty] private decimal usdDiff;
    [ObservableProperty] private bool usdUp;
    [ObservableProperty] private decimal eurRate;
    [ObservableProperty] private decimal eurDiff;
    [ObservableProperty] private bool eurUp;
    [ObservableProperty] private string rateDate = string.Empty;
    [ObservableProperty] private ObservableCollection<MetalPriceResponse> metals = [];

    public DashboardPageViewModel(IServiceProvider services)
    {
        dashboardApi = services.GetRequiredService<IDashboardApi>();
        marketDataApi = services.GetRequiredService<IMarketDataApi>();
        _ = LoadAsync();
        _ = LoadMarketAsync();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
        await LoadMarketAsync();
    }

    private async Task LoadAsync()
    {
        var response = await dashboardApi.GetAsync().Handle(loading => IsLoading = loading);
        if (!response.IsSuccess || response.Data is null)
        {
            Error = response.Message ?? "Dashboard ma'lumotlarini yuklashda xatolik!";
            return;
        }

        var d = response.Data;

        TodaySalesAmount = d.TodaySalesAmount;
        TodaySalesCount = d.TodaySalesCount;
        TodaySalesLength = d.TodaySalesLength;
        MonthSalesAmount = d.MonthSalesAmount;
        MonthSalesLength = d.MonthSalesLength;
        TodayPaymentsAmount = d.TodayPaymentsAmount;
        TodayPaymentsCount = d.TodayPaymentsCount;

        TopCustomers = new ObservableCollection<TopCustomerResponse>(d.TopCustomers);
        TopSellingProducts = new ObservableCollection<TopProductResponse>(d.TopSellingProducts);

        BuildWeeklyChart(d.WeeklySales);
    }

    private void BuildWeeklyChart(List<DailySalesResponse> week)
    {
        var line = new PointCollection();
        var points = new ObservableCollection<ChartPoint>();

        if (week.Count == 0)
        {
            WeeklyLinePoints = line;
            WeeklyAreaPoints = new PointCollection();
            WeeklyPoints = points;
            return;
        }

        var max = week.Max(x => x.Amount);
        var stepX = week.Count > 1 ? (ChartWidth - 2 * PadX) / (week.Count - 1) : 0;

        for (int i = 0; i < week.Count; i++)
        {
            var item = week[i];
            var x = PadX + i * stepX;
            var ratio = max > 0 ? (double)(item.Amount / max) : 0;
            var y = PadTop + (1 - ratio) * (ChartHeight - PadTop);

            line.Add(new Point(x, y));
            points.Add(new ChartPoint
            {
                X = x,
                Y = y,
                DayLabel = item.Date.ToString("dd.MM"),
                AmountText = item.Amount > 0 ? item.Amount.ToString("N0") : "0"
            });
        }

        // Chiziq tagidagi gradient maydon uchun pastki ikki burchakni qo'shamiz
        var area = new PointCollection(line);
        area.Add(new Point(line[^1].X, ChartHeight));
        area.Add(new Point(line[0].X, ChartHeight));

        WeeklyLinePoints = line;
        WeeklyAreaPoints = area;
        WeeklyPoints = points;
    }

    private async Task LoadMarketAsync()
    {
        // Tashqi saytlardan kelgani uchun alohida yuklaymiz - asosiy dashboardni kutib turmaydi.
        var response = await marketDataApi.GetAsync().Handle();
        if (!response.IsSuccess || response.Data is null)
            return;

        var m = response.Data;
        RateDate = m.RateDate;

        if (m.Usd is not null)
        {
            UsdRate = m.Usd.Rate;
            UsdDiff = m.Usd.Diff;
            UsdUp = m.Usd.Diff >= 0;
            HasRates = true;
        }

        if (m.Eur is not null)
        {
            EurRate = m.Eur.Rate;
            EurDiff = m.Eur.Diff;
            EurUp = m.Eur.Diff >= 0;
        }

        Metals = new ObservableCollection<MetalPriceResponse>(m.Metals);
    }
}

public record ChartPoint
{
    public double X { get; init; }
    public double Y { get; init; }
    public string DayLabel { get; init; } = string.Empty;
    public string AmountText { get; init; } = string.Empty;
}
