namespace VoltStream.WPF.Products.Models;

using CommunityToolkit.Mvvm.ComponentModel;
using VoltStream.WPF.Commons;

public partial class ProductItemViewModel : ViewModelBase
{
    [ObservableProperty] private string? category = string.Empty;
    [ObservableProperty] private string? name = string.Empty;
    [ObservableProperty] private decimal? rollLength;
    [ObservableProperty] private decimal? quantity;
    [ObservableProperty] private decimal? totalCount;
    [ObservableProperty] private string? unit;
    [ObservableProperty] private decimal? price;
    [ObservableProperty] private decimal? totalAmount;

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