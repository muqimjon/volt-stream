namespace VoltStream.WPF.Commons.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Sales.ViewModels;

public partial class SaleViewModel : ViewModelBase
{
    public long Id { get; set; }
    public long? BranchId { get; set; }
    public long CustomerOperationId { get; set; }
    public long DiscountOperationId { get; set; }
    public long CurrencyId { get; set; }
    public long CustomerId { get; set; }

    [ObservableProperty] private DateTimeOffset date;
    [ObservableProperty] private int rollCount;
    [ObservableProperty] private decimal length;
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private decimal discount;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private bool isDiscountApplied;
    [ObservableProperty] private CustomerOperationViewModel customerOperation = new();
    [ObservableProperty] private CurrencyViewModel currency = new();
    [ObservableProperty] private CustomerViewModel customer = new();
    [ObservableProperty] private ObservableCollection<SaleItemViewModel> items = [];
}
