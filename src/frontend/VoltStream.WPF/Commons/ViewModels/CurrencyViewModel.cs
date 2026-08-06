namespace VoltStream.WPF.Commons.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class CurrencyViewModel : ViewModelBase
{
    [ObservableProperty] private long id;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string symbol = string.Empty;
    [ObservableProperty] private decimal exchangeRate;
    [ObservableProperty] private bool isDefault;
    [ObservableProperty] private bool isActive;
    [ObservableProperty] private bool isEditable;
    [ObservableProperty] private bool isCash;
    [ObservableProperty] private int position;

    public bool IsRateEditable => IsEditable && !IsDefault;

    public CurrencyViewModel? selected;
    public CurrencyViewModel Selected
    {
        get => selected!;
        set
        {
            if (SetProperty(ref selected, value) && value is not null)
            {
                Id = value.Id;
                Name = value.Name;
                Code = value.Code;
                Symbol = value.Symbol;
                ExchangeRate = value.ExchangeRate;
                IsDefault = value.IsDefault;
                IsActive = value.IsActive;
                Position = value.Position;
            }
        }
    }
}