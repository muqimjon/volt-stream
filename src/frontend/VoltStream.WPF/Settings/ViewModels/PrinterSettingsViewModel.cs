namespace VoltStream.WPF.Settings.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.ViewModels;

public partial class PrinterSettingsViewModel : ViewModelBase
{
    private readonly ISessionService session;

    public PrinterSettingsViewModel(PrinterSettings settings, PrintAgentService agent, ISessionService session)
    {
        Settings = settings;
        Agent = agent;
        this.session = session;
        _ = RefreshPrinters();
    }

    public PrinterSettings Settings { get; }
    public PrintAgentService Agent { get; }

    public IReadOnlyList<int> CopyOptions { get; } = [1, 2, 3];
    public IReadOnlyList<string> PaperOptions { get; } = ["A4", "A5"];

    public bool HasAgentPermission => session.Has("print.agent");

    [ObservableProperty] private ObservableCollection<string> printers = [];

    [RelayCommand]
    private async Task RefreshPrinters()
    {
        IsLoading = true;
        var names = await PrintService.GetPrinterNamesAsync();
        Printers = [.. names];
        IsLoading = false;
    }

    [RelayCommand]
    private Task TestPrint() => Agent.TestPrintAsync();

    [RelayCommand]
    private async Task Save()
    {
        await Agent.RestartAsync();
        Success = TranslationSource.T("Printer.Saved");
    }
}
