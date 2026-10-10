namespace VoltStream.WPF.Commons.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using VoltStream.WPF.Commons.Services;

public partial class PrinterSettings : ObservableObject
{
    [ObservableProperty] private bool enabled;
    [ObservableProperty] private string? printerName;
    [ObservableProperty] private int copies = 1;
    [ObservableProperty] private string paper = "A4";
    [ObservableProperty] private ReceiptLayout layout = ReceiptLayout.Expanded;
    [ObservableProperty] private int priority = 1;
    [ObservableProperty] private long? branchId;
    [ObservableProperty] private string? branchName;
}
