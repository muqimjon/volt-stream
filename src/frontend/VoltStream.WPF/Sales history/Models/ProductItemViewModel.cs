namespace VoltStream.WPF.Sales_history.Models;

using CommunityToolkit.Mvvm.ComponentModel;
using VoltStream.WPF.Commons;

public partial class ProductItemViewModel : ViewModelBase
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public long ProductId { get; set; }
    public long CategoryId { get; set; }

    [ObservableProperty] private DateTimeOffset? operationDate;
    [ObservableProperty] private string? category = string.Empty;
    [ObservableProperty] private string? name = string.Empty;
    [ObservableProperty] private decimal? rollLength;
    [ObservableProperty] private decimal? quantity;
    [ObservableProperty] private decimal? totalCount;
    [ObservableProperty] private string? unit;
    [ObservableProperty] private decimal? price;
    [ObservableProperty] private decimal? totalAmount;
    [ObservableProperty] private string? customer = string.Empty;
    [ObservableProperty] private string? composition;

    partial void OnPriceChanged(decimal? value)
        => ReCalculateTotalAmount();

    partial void OnTotalCountChanged(decimal? value)
        => ReCalculateTotalAmount();

    private void ReCalculateTotalAmount()
    {
        if (Price > 0)
            TotalAmount = TotalCount * Price;
    }
}