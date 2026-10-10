namespace VoltStream.WPF.SaleDrafts.Models;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using ApiServices.Models.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Dialogs;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Messages;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Sales.ViewModels;
using VoltStream.WPF.Sales.Views;

public partial class SaleDraftsPageViewModel : ViewModelBase
{
    private readonly IServiceProvider services;
    private readonly ISaleDraftsApi draftsApi;
    private readonly BranchContext branchContext;
    private readonly INavigationService navigationService;

    public SaleDraftsPageViewModel(IServiceProvider services)
    {
        this.services = services;
        draftsApi = services.GetRequiredService<ISaleDraftsApi>();
        branchContext = services.GetRequiredService<BranchContext>();
        navigationService = services.GetRequiredService<INavigationService>();

        PropertyChangedEventManager.AddHandler(branchContext, (_, _) => _ = LoadAsync(), nameof(BranchContext.BranchId));
        WeakReferenceMessenger.Default.Register<EntityUpdatedMessage<string>>(this, static (r, m) =>
        {
            if (m.Value == "DraftsUpdated") _ = ((SaleDraftsPageViewModel)r).LoadAsync();
        });

        _ = LoadAsync();
    }

    [ObservableProperty] private ObservableCollection<SaleDraftResponse> drafts = [];

    [RelayCommand]
    private async Task Load() => await LoadAsync();

    private async Task LoadAsync()
    {
        var response = await draftsApi.GetOpenAsync(branchContext.BranchId).Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess) Drafts = new ObservableCollection<SaleDraftResponse>(response.Data!);
        else Error = response.Message ?? TranslationSource.T("Drafts.LoadError");
    }

    [RelayCommand]
    private async Task Receipt(SaleDraftResponse draft)
    {
        var response = await draftsApi.GetReceipt(draft.Id).Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess) ReceiptDocumentBuilder.Preview(response.Data!, false, services);
        else Error = response.Message ?? TranslationSource.T("Receipt.LoadError");
    }

    [RelayCommand]
    private async Task Confirm(SaleDraftResponse draft)
    {
        var response = await draftsApi.GetByIdAsync(draft.Id).Handle(isLoading => IsLoading = isLoading);
        if (!response.IsSuccess)
        {
            Error = response.Message ?? TranslationSource.T("Drafts.LoadError");
            return;
        }

        var detail = response.Data!;
        var items = await BuildItemsAsync(detail);
        if (items is null) return;

        var session = services.GetRequiredService<SaleSession>();
        session.Reset();

        var sale = session.Current;
        sale.SaleDraftId = detail.Id;
        sale.DraftBranchId = detail.BranchId;
        sale.DraftLabel = string.Format(TranslationSource.T("Drafts.Confirming"), detail.Id);
        sale.CustomerId = detail.CustomerId;
        sale.Description = detail.Description;

        // Savdo sahifasida qator ro'yxat boshiga qo'shiladi va yuborishda teskari aylantiriladi — shu tartib ro'yxat tartibini saqlaydi.
        foreach (var item in items)
            sale.SaleItems.Insert(0, item);

        navigationService.Navigate(services.GetRequiredService<SalesPage>());
    }

    [RelayCommand]
    private async Task Cancel(SaleDraftResponse draft)
    {
        if (!AppDialog.Confirm(string.Format(TranslationSource.T("Drafts.ConfirmCancel"), draft.Id), TranslationSource.T("Common.Cancel")))
            return;

        var response = await draftsApi.CancelAsync(draft.Id).Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess)
        {
            Success = TranslationSource.T("Drafts.Cancelled");
            await LoadAsync();
        }
        else Error = response.Message ?? TranslationSource.T("Drafts.CancelError");
    }

    // Ro'yxatda narx va chegirma yo'q: oddiy qator qo'shilgandagidek ombordagi qoldiqdan olinadi.
    private async Task<List<SaleItem>?> BuildItemsAsync(SaleDraftResponse draft)
    {
        var stocksApi = services.GetRequiredService<IWarehouseStocksApi>();
        var stocks = new Dictionary<long, List<WarehouseStockResponse>>();

        foreach (var productId in draft.Items.Select(i => i.ProductId).Distinct())
        {
            var response = await stocksApi.GetProductDetailsFromWarehouseAsync(productId, draft.BranchId).Handle(isLoading => IsLoading = isLoading);
            if (!response.IsSuccess)
            {
                Error = response.Message ?? TranslationSource.T("Sales.WarehouseLoadError");
                return null;
            }

            stocks[productId] = response.Data!;
        }

        var products = await services.GetRequiredService<IProductsApi>()
            .Filter(new FilteringRequest { Filters = new() { ["category"] = ["include"] } })
            .Handle(isLoading => IsLoading = isLoading);
        var weights = products.IsSuccess ? products.Data!.ToDictionary(p => p.Id, p => p.WeightPerUnit) : [];

        return [.. draft.Items.Select(line =>
        {
            var stock = stocks[line.ProductId].FirstOrDefault(s => s.LengthPerRoll == line.LengthPerRoll);
            var cut = line.TotalLength % line.LengthPerRoll;

            return new SaleItem
            {
                CategoryId = line.CategoryId,
                CategoryName = line.Category,
                ProductId = line.ProductId,
                ProductName = line.Product,
                PerRollCount = line.LengthPerRoll,
                WarehouseCountRoll = stock?.RollCount ?? 0,
                WarehouseQuantity = stock?.TotalLength ?? 0,
                RollCount = line.RollCount,
                Quantity = line.TotalLength,
                Weight = weights.GetValueOrDefault(line.ProductId) * line.TotalLength,
                NewQuantity = cut == 0 ? 0 : line.LengthPerRoll - cut,
                Price = stock?.UnitPrice ?? 0,
                PerDiscount = stock?.DiscountRate ?? 0
            };
        })];
    }
}
