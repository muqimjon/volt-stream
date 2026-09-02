namespace VoltStream.WPF.Settings.ViewModels;

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

public partial class BranchSettingsViewModel : ViewModelBase
{
    private readonly IBranchesApi branchesApi;

    public BranchSettingsViewModel(IServiceProvider services)
    {
        branchesApi = services.GetRequiredService<IBranchesApi>();
        _ = LoadBranches();
    }

    [ObservableProperty] private ObservableCollection<BranchResponse> branches = [];
    [ObservableProperty] private BranchResponse? selectedBranch;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private bool isEditing;

    public async Task LoadBranches()
    {
        var response = await branchesApi.GetAllAsync().Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess) Branches = new ObservableCollection<BranchResponse>(response.Data);
        else Error = response.Message ?? TranslationSource.T("Branches.LoadError");
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;

        if (IsEditing && SelectedBranch != null)
        {
            var response = await branchesApi.UpdateAsync(new BranchRequest { Id = SelectedBranch.Id, Name = Name, IsActive = IsActive })
                .Handle(isLoading => IsLoading = isLoading);

            if (response.IsSuccess)
            {
                Success = TranslationSource.T("Branches.Updated");
                await LoadBranches();
                Cancel();
            }
            else Error = response.Message ?? TranslationSource.T("Branches.UpdateError");
        }
        else
        {
            var response = await branchesApi.CreateAsync(new BranchRequest { Name = Name, IsActive = IsActive })
                .Handle(isLoading => IsLoading = isLoading);

            if (response.IsSuccess)
            {
                Success = TranslationSource.T("Branches.Created");
                await LoadBranches();
                Cancel();
            }
            else Error = response.Message ?? TranslationSource.T("Branches.CreateError");
        }
    }

    [RelayCommand]
    private void Edit(BranchResponse branch)
    {
        SelectedBranch = branch;
        Name = branch.Name;
        IsActive = branch.IsActive;
        IsEditing = true;
    }

    [RelayCommand]
    private void Cancel()
    {
        SelectedBranch = null;
        Name = string.Empty;
        IsActive = true;
        IsEditing = false;
    }
}
