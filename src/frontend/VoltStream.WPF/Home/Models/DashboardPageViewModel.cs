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
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Home.Controls;

public partial class DashboardPageViewModel : ViewModelBase
{
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrendTitle))]
    private DashboardInterval interval = DashboardInterval.Week;

    private DateTime beginDate = DateTime.Today.AddDays(-6);
    private DateTime endDate = DateTime.Today;

    public string TrendTitle => Interval switch
    {
        DashboardInterval.Day => TranslationSource.T("Dashboard.TrendToday"),
        DashboardInterval.Month => TranslationSource.T("Dashboard.TrendMonth"),
        _ => TranslationSource.T("Dashboard.TrendWeek"),
    };

    [ObservableProperty] private ChartData chart = new();

    // Tashqi bozor ma'lumotlari (CBU valyuta kursi + investing.com metall narxlari)
    [ObservableProperty] private bool hasRates;
    [ObservableProperty] private bool marketLoadFailed;
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

    partial void OnIntervalChanged(DashboardInterval value)
    {
        endDate = DateTime.Today;
        beginDate = value switch
        {
            DashboardInterval.Day => DateTime.Today,
            DashboardInterval.Month => DateTime.Today.AddDays(-29),
            _ => DateTime.Today.AddDays(-6),
        };
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
        await LoadMarketAsync();
    }

    private async Task LoadAsync()
    {
        var response = await dashboardApi.GetAsync(beginDate, endDate).Handle(loading => IsLoading = loading);
        if (!response.IsSuccess || response.Data is null)
        {
            Error = response.Message ?? TranslationSource.T("Dashboard.LoadError");
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

        Chart = new ChartData
        {
            Labels = d.WeeklySales.Select(x => x.Label).ToList(),
            Series =
            [
                new ChartSeries
                {
                    Name = TranslationSource.T("Dashboard.SeriesSalesAmount"),
                    ColorKey = "BrandColor",
                    Values = d.WeeklySales.Select(x => (double)x.Amount).ToList()
                },
                new ChartSeries
                {
                    Name = TranslationSource.T("Dashboard.SeriesIncome"),
                    ColorKey = "SuccessColor",
                    Values = d.WeeklyPayments.Select(x => (double)x.Amount).ToList()
                }
            ]
        };
    }

    private async Task LoadMarketAsync()
    {
        // Tashqi saytlardan kelgani uchun alohida yuklaymiz - asosiy dashboardni kutib turmaydi.
        var response = await marketDataApi.GetAsync().Handle();
        if (!response.IsSuccess || response.Data is null)
        {
            MarketLoadFailed = true;
            return;
        }

        MarketLoadFailed = false;
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

public enum DashboardInterval { Day, Week, Month }
