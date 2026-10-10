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
using VoltStream.WPF.Commons.Dialogs;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Messages;

public partial class WarehouseSettingsViewModel : ViewModelBase
{
    public const string UpdatedMessage = "WarehousesUpdated";

    private readonly IWarehousesApi warehousesApi;

    public WarehouseSettingsViewModel(IServiceProvider services)
    {
        warehousesApi = services.GetRequiredService<IWarehousesApi>();
        _ = LoadWarehouses();
    }

    [ObservableProperty] private ObservableCollection<WarehouseResponse> warehouses = [];
    [ObservableProperty] private WarehouseResponse? selectedWarehouse;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private bool isEditing;

    public async Task LoadWarehouses()
    {
        var response = await warehousesApi.GetAllAsync().Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess) Warehouses = new ObservableCollection<WarehouseResponse>(response.Data!);
        else Error = response.Message ?? TranslationSource.T("Warehouses.LoadError");
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;

        if (IsEditing && SelectedWarehouse != null)
        {
            var response = await warehousesApi.UpdateAsync(new WarehouseRequest { Id = SelectedWarehouse.Id, Name = Name })
                .Handle(isLoading => IsLoading = isLoading);

            if (response.IsSuccess)
            {
                Success = TranslationSource.T("Warehouses.Updated");
                await Refresh();
            }
            else Error = response.Message ?? TranslationSource.T("Warehouses.UpdateError");
        }
        else
        {
            var response = await warehousesApi.CreateAsync(new WarehouseRequest { Name = Name })
                .Handle(isLoading => IsLoading = isLoading);

            if (response.IsSuccess)
            {
                Success = TranslationSource.T("Warehouses.Created");
                await Refresh();
            }
            else Error = response.Message ?? TranslationSource.T("Warehouses.CreateError");
        }
    }

    [RelayCommand]
    private void Edit(WarehouseResponse warehouse)
    {
        SelectedWarehouse = warehouse;
        Name = warehouse.Name;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task Delete(WarehouseResponse warehouse)
    {
        if (!AppDialog.Confirm($"{warehouse.Name} {TranslationSource.T("Warehouses.ConfirmDelete")}", TranslationSource.T("Common.Delete"))) return;

        var response = await warehousesApi.DeleteAsync(warehouse.Id).Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess)
        {
            Success = TranslationSource.T("Warehouses.Deleted");
            await Refresh();
        }
        else Error = response.Message ?? TranslationSource.T("Warehouses.DeleteError");
    }

    [RelayCommand]
    private void Cancel()
    {
        SelectedWarehouse = null;
        Name = string.Empty;
        IsEditing = false;
    }

    private async Task Refresh()
    {
        await LoadWarehouses();
        Cancel();
        WeakReferenceMessenger.Default.Send(new EntityUpdatedMessage<string>(UpdatedMessage));
    }
}
