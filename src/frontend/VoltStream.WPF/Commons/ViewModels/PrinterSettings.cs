namespace VoltStream.WPF.Commons.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class PrinterSettings : ObservableObject
{
    [ObservableProperty] private bool enabled;
    [ObservableProperty] private string? printerName;
    [ObservableProperty] private int copies = 1;
    [ObservableProperty] private string paper = "A4";
    [ObservableProperty] private int priority = 1;
    [ObservableProperty] private long? branchId;
    [ObservableProperty] private string? branchName;
}
