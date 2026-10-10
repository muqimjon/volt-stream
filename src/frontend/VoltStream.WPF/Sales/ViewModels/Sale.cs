namespace VoltStream.WPF.Sales.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using VoltStream.WPF.Commons;

public partial class Sale : ViewModelBase
{
    [ObservableProperty] private decimal? totalSum;
    [ObservableProperty] private decimal? totalDiscount;
    [ObservableProperty] private decimal? finalSum;

    [ObservableProperty] private DateTime operationDate = DateTime.Now;
    [ObservableProperty] private long? customerId;
    [ObservableProperty] private string customerName = string.Empty;
    [ObservableProperty] private long currencyId;
    [ObservableProperty] private bool isDiscountApplied = false;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private long? saleDraftId;
    [ObservableProperty] private long? draftBranchId;
    [ObservableProperty] private string draftLabel = string.Empty;
    [ObservableProperty] private string categoryName = string.Empty;
    [ObservableProperty] private long categoryId;
    [ObservableProperty] private long productId;
    [ObservableProperty] private string? productName;
    [ObservableProperty] private string? perRollCount;
    [ObservableProperty] private string? rollCount;
    [ObservableProperty] private decimal warehouseCountRoll = decimal.Zero;
    [ObservableProperty] private string? quantity;
    [ObservableProperty] private decimal newQuantity = decimal.Zero;
    [ObservableProperty] private decimal warehouseQuantity = decimal.Zero;
    [ObservableProperty] private string? price;
    [ObservableProperty] private string? sum;
    [ObservableProperty] private string? perDiscount;
    [ObservableProperty] private string? discount;
    [ObservableProperty] private string? finalSumProduct;

    public ObservableCollection<SaleItem> SaleItems { get; set; } = [];

    public Sale()
    {
        SaleItems.CollectionChanged += SaleItems_CollectionChanged;
    }

    private void SaleItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (SaleItem item in e.OldItems)
                item.PropertyChanged -= OnSaleItemPropertyChanged;

        if (e.NewItems != null)
            foreach (SaleItem item in e.NewItems)
                item.PropertyChanged += OnSaleItemPropertyChanged;

        RecalculateTotals();
    }

    private void OnSaleItemPropertyChanged(object? sender, PropertyChangedEventArgs e) => RecalculateTotals();

    partial void OnIsDiscountAppliedChanged(bool value) => RecalculateTotals();

    private void RecalculateTotals()
    {
        if (SaleItems.Count == 0)
        {
            TotalSum = 0;
            TotalDiscount = 0;
            FinalSum = 0;
            return;
        }

        TotalSum = SaleItems.Sum(x => x.Sum ?? 0);
        TotalDiscount = SaleItems.Sum(x => x.Discount ?? 0);

        if (IsDiscountApplied)
            FinalSum = TotalSum - TotalDiscount;
        else
            FinalSum = TotalSum;
    }
}
