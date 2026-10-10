namespace VoltStream.WPF.Commons.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class PaymentViewModel : ViewModelBase
{
    public long Id { get; set; }
    public long CurrencyId { get; set; }
    public long CustomerId { get; set; }

    [ObservableProperty] private DateTime? paidAt = DateTime.Now;
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private decimal exchangeRate;
    [ObservableProperty] private decimal netAmount;
    [ObservableProperty] private decimal? discount;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private CurrencyViewModel currency = default!;
    [ObservableProperty] private CustomerViewModel customer = default!;

    [ObservableProperty] private decimal? incomeAmount;
    [ObservableProperty] private decimal? expenseAmount;

    [ObservableProperty] private decimal? lastBalance;
    [ObservableProperty] private decimal? balance;

    [ObservableProperty] private bool isIncomeEnabled = true;
    [ObservableProperty] private bool isExpenseEnabled = true;
}