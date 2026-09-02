namespace VoltStream.WPF.Settings.ViewModels;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using ApiServices.Models.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.ViewModels;

public partial class AuditLogViewModel : ViewModelBase
{
    private static readonly string[] KnownActions =
    [
        "Login", "CreateSale", "UpdateSale", "CreateSupply", "UpdateSupply", "DeleteSupply",
        "CreatePayment", "DeleteCustomerOperation", "CreateProduct", "UpdateProduct",
        "CreateUser", "UpdateUser", "CreatePrintJob"
    ];

    private readonly IAuditLogsApi auditLogsApi;
    private readonly IUsersApi usersApi;
    private readonly IBranchesApi branchesApi;
    private readonly ISessionService session;
    private readonly string anyLabel = TranslationSource.T("Common.All");

    private bool suppressReload;

    public AuditLogViewModel(IServiceProvider services)
    {
        auditLogsApi = services.GetRequiredService<IAuditLogsApi>();
        usersApi = services.GetRequiredService<IUsersApi>();
        branchesApi = services.GetRequiredService<IBranchesApi>();
        session = services.GetRequiredService<ISessionService>();

        Pagination = new PaginationViewModel(LoadPageAsync);

        Actions = [anyLabel, .. KnownActions];
        selectedAction = anyLabel;

        _ = LoadInitialDataAsync();
    }

    public PaginationViewModel Pagination { get; }

    public bool CanSelectBranch => session.IsAdmin;

    [ObservableProperty] private ObservableCollection<AuditLogResponse> logs = [];
    [ObservableProperty] private ObservableCollection<UserResponse> users = [];
    [ObservableProperty] private ObservableCollection<BranchResponse> branches = [];
    [ObservableProperty] private ObservableCollection<string> actions = [];
    [ObservableProperty] private UserResponse? selectedUser;
    [ObservableProperty] private BranchResponse? selectedBranch;
    [ObservableProperty] private string? selectedAction;
    [ObservableProperty] private string? search;
    [ObservableProperty] private DateTime beginDate = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime endDate = DateTime.Today;

    #region Property Changes

    partial void OnSelectedUserChanged(UserResponse? value) => TriggerReload();

    partial void OnSelectedBranchChanged(BranchResponse? value) => TriggerReload();

    partial void OnSelectedActionChanged(string? value) => TriggerReload();

    partial void OnBeginDateChanged(DateTime value) => TriggerReload();

    partial void OnEndDateChanged(DateTime value) => TriggerReload();

    partial void OnSearchChanged(string? value) => TriggerReload();

    private void TriggerReload()
    {
        if (!suppressReload) _ = ReloadAsync();
    }

    #endregion Property Changes

    #region Load Data

    private async Task LoadInitialDataAsync()
    {
        await LoadUsersAsync();
        if (CanSelectBranch) await LoadBranchesAsync();
        await ReloadAsync();
    }

    private async Task LoadUsersAsync()
    {
        var response = await usersApi.GetAllAsync().Handle();
        if (!response.IsSuccess) return;

        suppressReload = true;
        Users = [new UserResponse { Id = 0, Username = anyLabel }, .. response.Data];
        SelectedUser = Users[0];
        suppressReload = false;
    }

    private async Task LoadBranchesAsync()
    {
        var response = await branchesApi.GetAllAsync().Handle();
        if (!response.IsSuccess) return;

        suppressReload = true;
        Branches = [new BranchResponse { Id = 0, Name = anyLabel }, .. response.Data];
        SelectedBranch = Branches[0];
        suppressReload = false;
    }

    private async Task ReloadAsync()
    {
        Pagination.Reset();
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        var request = BuildFilters();
        request.Page = Pagination.Page;
        request.PageSize = Pagination.PageSize;

        Response<List<AuditLogResponse>> response;
        using (PagingScope.Begin())
        {
            response = await auditLogsApi.Filter(request).Handle(l => IsLoading = l);
            if (response.IsSuccess) Pagination.Apply(PagingScope.Result);
        }

        if (!response.IsSuccess)
        {
            Error = response.Message ?? TranslationSource.T("Logs.LoadError");
            return;
        }

        Logs = [.. response.Data];
    }

    private async Task<List<AuditLogResponse>> LoadAllAsync()
    {
        var response = await auditLogsApi.Filter(BuildFilters()).Handle(l => IsLoading = l);
        return response.IsSuccess ? response.Data : [];
    }

    private FilteringRequest BuildFilters()
    {
        var filters = new Dictionary<string, List<string>>
        {
            ["Timestamp"] = [$">={BeginDate:yyyy-MM-dd}", $"<{EndDate.AddDays(1):yyyy-MM-dd}"]
        };

        if (SelectedUser is { Id: > 0 } user)
            filters["Username"] = [$"={user.Username}"];

        if (SelectedBranch is { Id: > 0 } branch)
            filters["BranchId"] = [$"={branch.Id}"];

        if (!string.IsNullOrWhiteSpace(SelectedAction) && SelectedAction != anyLabel)
            filters["Action"] = [$"={SelectedAction}"];

        return new FilteringRequest
        {
            Filters = filters,
            Search = string.IsNullOrWhiteSpace(Search) ? null : Search,
            SortBy = "Timestamp",
            Descending = true
        };
    }

    #endregion Load Data

    #region Commands

    [RelayCommand]
    private Task Reload() => ReloadAsync();

    [RelayCommand]
    private void ClearFilter()
    {
        suppressReload = true;
        SelectedUser = Users.FirstOrDefault();
        SelectedBranch = Branches.FirstOrDefault();
        SelectedAction = anyLabel;
        Search = null;
        BeginDate = DateTime.Today.AddDays(-7);
        EndDate = DateTime.Today;
        suppressReload = false;
        _ = ReloadAsync();
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        var definition = await BuildReportAsync();
        if (definition is null) return;
        if (ReportService.ExportExcel(definition)) Success = TranslationSource.T("Logs.ExcelSaved");
    }

    [RelayCommand]
    private async Task Print()
    {
        var definition = await BuildReportAsync();
        if (definition is not null) ReportService.Print(definition);
    }

    [RelayCommand]
    private async Task Preview()
    {
        var definition = await BuildReportAsync();
        if (definition is not null) ReportService.Preview(definition);
    }

    private async Task<ReportDefinition<AuditLogResponse>?> BuildReportAsync()
    {
        var rows = await LoadAllAsync();
        if (rows.Count == 0)
        {
            Info = TranslationSource.T("Logs.NoData");
            return null;
        }

        return new ReportDefinition<AuditLogResponse>
        {
            Title = TranslationSource.T("Logs.ReportTitle"),
            Subtitle = $"{BeginDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}",
            SheetName = TranslationSource.T("Settings.TabLogs"),
            FileName = $"Loglar-{BeginDate:dd.MM.yyyy}-{EndDate:dd.MM.yyyy}",
            Rows = rows,
            Columns =
            [
                new() { Header = TranslationSource.T("Logs.Time"), Width = 110, Value = r => r.Timestamp.LocalDateTime.ToString("dd.MM.yyyy HH:mm:ss") },
                new() { Header = TranslationSource.T("Logs.User"), Width = 95, Value = r => r.Username },
                new() { Header = TranslationSource.T("Logs.Branch"), Width = 90, Value = r => r.BranchName },
                new() { Header = TranslationSource.T("Logs.Action"), Width = 130, Value = r => r.Action },
                new() { Header = TranslationSource.T("Logs.Entity"), Width = 110, Value = r => r.EntityName },
                new() { Header = "Id", Width = 55, Align = ReportAlign.Right, Value = r => r.EntityId },
                new() { Header = TranslationSource.T("Logs.Source"), Width = 70, Value = r => r.Source },
                new() { Header = TranslationSource.T("Logs.Status"), Width = 70, Align = ReportAlign.Center, Value = r => r.Success ? "✓" : "✗" }
            ]
        };
    }

    #endregion Commands
}
