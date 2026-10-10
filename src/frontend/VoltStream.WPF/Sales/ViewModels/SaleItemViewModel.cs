namespace VoltStream.WPF.Sales.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.ViewModels;

public partial class SaleItemViewModel : ViewModelBase
{
    public long Id { get; set; }
    public long SaleId { get; set; }
    public long ProductId { get; set; }
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasRollMismatch))] private int? rollCount;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasRollMismatch))] private decimal? lengthPerRoll;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasRollMismatch))] private decimal? totalLength;
    [ObservableProperty] private decimal? unitPrice;
    [ObservableProperty] private decimal? discountRate;
    [ObservableProperty] private decimal? discountAmount;
    [ObservableProperty] private decimal? totalAmount;
    [ObservableProperty] private decimal? finalAmount;

    [ObservableProperty] private SaleViewModel sale = new();
    [ObservableProperty] private ProductViewModel product = new();

    // Server shu shartni qat'iy talab qiladi: kesilgan bo'lak musbat va to'liq rulondan oshmaydi (RollComposition.IsConsistent).
    public bool HasRollMismatch => RollCount is > 0 && LengthPerRoll is > 0 && TotalLength is { } total
        && !(total > (RollCount - 1) * LengthPerRoll && total <= RollCount * LengthPerRoll);
}
