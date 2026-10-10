namespace VoltStream.WPF.Settings.ViewModels;

using ApiServices.Enums;
using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;

public partial class PermissionItemViewModel : ObservableObject
{
    public string Key { get; init; } = string.Empty;
    [ObservableProperty] private bool isChecked;
}

public class PermissionGroupViewModel
{
    public string Group { get; init; } = string.Empty;
    public List<PermissionItemViewModel> Items { get; init; } = [];
}

public partial class UserSettingsViewModel : ViewModelBase
{
    private readonly IUsersApi usersApi;
    private readonly IBranchesApi branchesApi;
    private readonly IPermissionsApi permissionsApi;

    public UserSettingsViewModel(IServiceProvider services)
    {
        usersApi = services.GetRequiredService<IUsersApi>();
        branchesApi = services.GetRequiredService<IBranchesApi>();
        permissionsApi = services.GetRequiredService<IPermissionsApi>();
        _ = LoadData();
    }

    [ObservableProperty] private ObservableCollection<UserResponse> users = [];
    [ObservableProperty] private UserResponse? selectedUser;
    [ObservableProperty] private ObservableCollection<BranchResponse> branches = [];
    [ObservableProperty] private ObservableCollection<PermissionGroupViewModel> permissionGroups = [];

    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string? phone;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private UserRole role = UserRole.Seller;
    [ObservableProperty] private BranchResponse? selectedBranch;
    [ObservableProperty] private PrintPolicy printPolicy = PrintPolicy.Auto;
    [ObservableProperty] private bool isSettingPassword;
    [ObservableProperty] private string newPassword = string.Empty;

    public IReadOnlyList<UserRole> Roles { get; } = Enum.GetValues<UserRole>();
    public IReadOnlyList<PrintPolicy> PrintPolicies { get; } = Enum.GetValues<PrintPolicy>();

    public bool IsAdminRole => Role == UserRole.Admin;

    partial void OnRoleChanged(UserRole value) => OnPropertyChanged(nameof(IsAdminRole));

    #region Load Data

    private async Task LoadData() => await Task.WhenAll(LoadUsers(), LoadBranches(), LoadPermissions());

    private async Task LoadUsers()
    {
        var response = await usersApi.GetAllAsync().Handle(l => IsLoading = l);
        if (response.IsSuccess) Users = new ObservableCollection<UserResponse>(response.Data);
        else Error = response.Message ?? TranslationSource.T("Users.LoadError");
    }

    private async Task LoadBranches()
    {
        var response = await branchesApi.GetAllAsync().Handle();
        if (!response.IsSuccess) return;

        ObservableCollection<BranchResponse> list = [new() { Id = 0, Name = TranslationSource.T("Users.NoBranch") }, .. response.Data];
        Branches = list;
        SelectedBranch = Branches[0];
    }

    private async Task LoadPermissions()
    {
        var response = await permissionsApi.GetAllAsync().Handle();
        if (!response.IsSuccess)
        {
            Error = response.Message ?? TranslationSource.T("Users.PermissionsLoadError");
            return;
        }

        PermissionGroups = new ObservableCollection<PermissionGroupViewModel>(response.Data!
            .GroupBy(p => p.Group)
            .Select(g => new PermissionGroupViewModel
            {
                Group = g.Key,
                Items = [.. g.Select(p => new PermissionItemViewModel { Key = p.Key })]
            }));
    }

    #endregion Load Data

    #region Commands

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Name))
        {
            Warning = TranslationSource.T("Users.UsernameNameRequired");
            return;
        }

        if (!IsEditing && string.IsNullOrWhiteSpace(Password))
        {
            Warning = TranslationSource.T("Users.PasswordRequired");
            return;
        }

        string[] permissions = Role == UserRole.Admin
            ? []
            : [.. PermissionGroups.SelectMany(g => g.Items).Where(i => i.IsChecked).Select(i => i.Key)];

        var request = new UserRequest
        {
            Id = IsEditing && SelectedUser is not null ? SelectedUser.Id : 0,
            Username = Username,
            Password = Password,
            Name = Name,
            Role = Role,
            BranchId = SelectedBranch is { Id: > 0 } ? SelectedBranch.Id : null,
            Permissions = permissions,
            PrintPolicy = PrintPolicy,
            Phone = Phone,
            IsActive = IsActive
        };

        if (IsEditing)
        {
            var response = await usersApi.UpdateAsync(request).Handle(l => IsLoading = l);
            if (response.IsSuccess)
            {
                Success = TranslationSource.T("Users.Updated");
                await LoadUsers();
                Cancel();
            }
            else Error = response.Message ?? TranslationSource.T("Users.UpdateError");
        }
        else
        {
            var response = await usersApi.CreateAsync(request).Handle(l => IsLoading = l);
            if (response.IsSuccess)
            {
                Success = TranslationSource.T("Users.Created");
                await LoadUsers();
                Cancel();
            }
            else Error = response.Message ?? TranslationSource.T("Users.CreateError");
        }
    }

    [RelayCommand]
    private void Edit(UserResponse user)
    {
        SelectedUser = user;
        Username = user.Username;
        Name = user.Name ?? string.Empty;
        Phone = user.Phone;
        Role = user.Role;
        PrintPolicy = user.PrintPolicy;
        IsActive = user.IsActive;
        SelectedBranch = Branches.FirstOrDefault(b => b.Id == user.BranchId) ?? Branches.FirstOrDefault();
        Password = string.Empty;

        foreach (var item in PermissionGroups.SelectMany(g => g.Items))
            item.IsChecked = user.Permissions.Contains(item.Key);

        IsEditing = true;
        IsSettingPassword = false;
    }

    [RelayCommand]
    private void StartSetPassword() => IsSettingPassword = true;

    [RelayCommand]
    private async Task SavePassword()
    {
        if (SelectedUser is null || string.IsNullOrWhiteSpace(NewPassword))
        {
            Warning = TranslationSource.T("Users.PasswordRequired");
            return;
        }

        var response = await usersApi.UpdatePasswordAsync(SelectedUser.Id, new PasswordRequest(NewPassword))
            .Handle(l => IsLoading = l);

        if (response.IsSuccess)
        {
            Success = TranslationSource.T("Users.PasswordUpdated");
            NewPassword = string.Empty;
            IsSettingPassword = false;
        }
        else Error = response.Message ?? TranslationSource.T("Users.PasswordUpdateError");
    }

    [RelayCommand]
    private void Cancel()
    {
        SelectedUser = null;
        Username = string.Empty;
        Name = string.Empty;
        Phone = null;
        Password = string.Empty;
        NewPassword = string.Empty;
        Role = UserRole.Seller;
        PrintPolicy = PrintPolicy.Auto;
        IsActive = true;
        SelectedBranch = Branches.FirstOrDefault();
        IsEditing = false;
        IsSettingPassword = false;

        foreach (var item in PermissionGroups.SelectMany(g => g.Items))
            item.IsChecked = false;
    }

    #endregion Commands
}
