namespace VoltStream.WPF.Settings.ViewModels;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models.Responses;
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
    private readonly IBranchesApi branchesApi;

    public PrinterSettingsViewModel(PrinterSettings settings, PrintAgentService agent, ISessionService session, IBranchesApi branchesApi)
    {
        Settings = settings;
        Agent = agent;
        this.session = session;
        this.branchesApi = branchesApi;
        _ = RefreshPrinters();
        _ = LoadBranchesAsync();
    }

    public PrinterSettings Settings { get; }
    public PrintAgentService Agent { get; }

    public IReadOnlyList<int> CopyOptions { get; } = [1, 2, 3];
    public IReadOnlyList<string> PaperOptions { get; } = ["A4", "A5"];
    public IReadOnlyList<int> PriorityOptions { get; } = [1, 2, 3, 4, 5];

    public bool HasAgentPermission => session.Has("print.agent");

    public bool HasBranch => Settings.BranchId is not null;

    [ObservableProperty] private ObservableCollection<BranchResponse> branches = [];

    [ObservableProperty] private BranchResponse? selectedBranch;

    partial void OnSelectedBranchChanged(BranchResponse? value)
    {
        Settings.BranchId = value?.Id;
        Settings.BranchName = value?.Name;
        OnPropertyChanged(nameof(HasBranch));
    }

    private async Task LoadBranchesAsync()
    {
        var response = await branchesApi.GetAllAsync().Handle();
        if (!response.IsSuccess) return;

        Branches = [.. response.Data];
        SelectedBranch = Branches.FirstOrDefault(branch => branch.Id == Settings.BranchId);
    }

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
