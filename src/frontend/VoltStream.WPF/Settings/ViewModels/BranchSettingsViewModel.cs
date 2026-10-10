namespace VoltStream.WPF.Settings.ViewModels;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Messages;

public partial class BranchSettingsViewModel : ViewModelBase
{
    public const string BranchesUpdatedMessage = "BranchesUpdated";

    private readonly IBranchesApi branchesApi;
    private readonly IWarehousesApi warehousesApi;
    private List<WarehouseResponse> allWarehouses = [];

    public BranchSettingsViewModel(IServiceProvider services)
    {
        branchesApi = services.GetRequiredService<IBranchesApi>();
        warehousesApi = services.GetRequiredService<IWarehousesApi>();

        WeakReferenceMessenger.Default.Register<EntityUpdatedMessage<string>>(this, static (r, m) =>
        {
            if (m.Value == WarehouseSettingsViewModel.UpdatedMessage) _ = ((BranchSettingsViewModel)r).Reload();
        });

        _ = Reload();
    }

    [ObservableProperty] private ObservableCollection<BranchResponse> branches = [];
    [ObservableProperty] private BranchResponse? selectedBranch;
    [ObservableProperty] private ObservableCollection<WarehouseResponse> warehouseOptions = [];
    [ObservableProperty] private WarehouseResponse? selectedWarehouse;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private bool isEditing;

    public async Task LoadBranches()
    {
        var response = await branchesApi.GetAllAsync().Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess) Branches = new ObservableCollection<BranchResponse>(response.Data);
        else Error = response.Message ?? TranslationSource.T("Branches.LoadError");
    }

    private async Task Reload()
    {
        var response = await warehousesApi.GetAllAsync().Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess)
        {
            allWarehouses = response.Data!;
            ShowWarehouseOptions(SelectedWarehouse?.Id);
        }
        else Error = response.Message ?? TranslationSource.T("Warehouses.LoadError");

        await LoadBranches();
    }

    // Tahrirda "yangi ombor" varianti yo'q: server uni "o'zgarmaydi" deb tushunadi.
    private void ShowWarehouseOptions(long? selectedId)
    {
        List<WarehouseResponse> options = [.. allWarehouses];
        if (!IsEditing) options.Insert(0, new WarehouseResponse { Name = TranslationSource.T("Branches.NewWarehouse") });

        WarehouseOptions = new ObservableCollection<WarehouseResponse>(options);
        SelectedWarehouse = options.FirstOrDefault(w => w.Id == selectedId) ?? options.FirstOrDefault();
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;

        long? warehouseId = SelectedWarehouse is { Id: > 0 } warehouse ? warehouse.Id : null;

        if (IsEditing && SelectedBranch != null)
        {
            var response = await branchesApi.UpdateAsync(new BranchRequest { Id = SelectedBranch.Id, Name = Name, IsActive = IsActive, WarehouseId = warehouseId })
                .Handle(isLoading => IsLoading = isLoading);

            if (response.IsSuccess)
            {
                Success = TranslationSource.T("Branches.Updated");
                await LoadBranches();
                Cancel();
                WeakReferenceMessenger.Default.Send(new EntityUpdatedMessage<string>(BranchesUpdatedMessage));
            }
            else Error = response.Message ?? TranslationSource.T("Branches.UpdateError");
        }
        else
        {
            var response = await branchesApi.CreateAsync(new BranchRequest { Name = Name, IsActive = IsActive, WarehouseId = warehouseId })
                .Handle(isLoading => IsLoading = isLoading);

            if (response.IsSuccess)
            {
                Success = TranslationSource.T("Branches.Created");
                await Reload();
                Cancel();
                WeakReferenceMessenger.Default.Send(new EntityUpdatedMessage<string>(BranchesUpdatedMessage));
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
        ShowWarehouseOptions(branch.WarehouseId);
    }

    [RelayCommand]
    private void Cancel()
    {
        SelectedBranch = null;
        Name = string.Empty;
        IsActive = true;
        IsEditing = false;
        ShowWarehouseOptions(null);
    }
}
