namespace VoltStream.WPF.Settings.ViewModels;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;

public partial class ProductSettingsViewModel : ViewModelBase
{
    private readonly IProductsApi productsApi;
    private readonly ICategoriesApi categoriesApi;

    public ProductSettingsViewModel(IServiceProvider services)
    {
        productsApi = services.GetRequiredService<IProductsApi>();
        categoriesApi = services.GetRequiredService<ICategoriesApi>();
        _ = LoadData();
    }

    [ObservableProperty] private ObservableCollection<ProductResponse> products = [];
    [ObservableProperty] private ObservableCollection<CategoryResponse> categories = [];
    [ObservableProperty] private ProductResponse? selectedProduct;
    [ObservableProperty] private CategoryResponse? selectedCategory;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string unit = string.Empty;
    [ObservableProperty] private bool isEditing;

    public async Task LoadData()
    {
        await Task.WhenAll(LoadProducts(), LoadCategories());
    }

    private async Task LoadProducts()
    {
        FilteringRequest request = new()
        {
            Filters = new()
            {
                ["category"] = ["include"]
            },
            SortBy = "name"
        };

        var response = await productsApi.Filter(request).Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess)
            Products = new ObservableCollection<ProductResponse>(response.Data);
        else
            Error = response.Message ?? TranslationSource.T("Settings.LoadProductsError");
    }

    private async Task LoadCategories()
    {
        var response = await categoriesApi.GetAllAsync().Handle();
        if (response.IsSuccess)
            Categories = new ObservableCollection<CategoryResponse>(response.Data);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name) || SelectedCategory == null) return;

        if (IsEditing && SelectedProduct != null)
        {
            var request = new ProductRequest
            {
                Id = SelectedProduct.Id,
                Name = Name,
                Unit = Unit,
                CategoryId = SelectedCategory.Id
            };

            var response = await productsApi.UpdateAsync(request).Handle(isLoading => IsLoading = isLoading);

            if (response.IsSuccess)
            {
                Success = TranslationSource.T("Settings.SavedSuccess");
                await LoadProducts();
                Cancel();
            }
            else
            {
                Error = response.Message ?? TranslationSource.T("Settings.ProductUpdateError");
            }
        }
        else
        {
            var request = new ProductRequest
            {
                Name = Name,
                Unit = Unit,
                CategoryId = SelectedCategory.Id
            };

            var response = await productsApi.CreateAsync(request).Handle(isLoading => IsLoading = isLoading);

            if (response.IsSuccess)
            {
                Success = TranslationSource.T("Settings.CreatedSuccess");
                await LoadProducts();
                Cancel();
            }
            else
            {
                Error = response.Message ?? TranslationSource.T("Settings.ProductCreateError");
            }
        }
    }

    [RelayCommand]
    private void Edit(ProductResponse product)
    {
        SelectedProduct = product;
        Name = product.Name;
        Unit = product.Unit;
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == product.CategoryId);
        IsEditing = true;
    }

    [RelayCommand]
    private async Task Delete(ProductResponse product)
    {
        if (MessageBox.Show($"{product.Name} {TranslationSource.T("Settings.ConfirmDeleteProduct")}", TranslationSource.T("Common.Delete"), MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

        var response = await productsApi.DeleteAsync(product.Id).Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
        {
            Success = TranslationSource.T("Settings.DeletedSuccess");
            await LoadProducts();
            if (SelectedProduct == product) Cancel();
        }
        else
        {
            Error = response.Message ?? TranslationSource.T("Settings.ProductDeleteError");
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        SelectedProduct = null;
        SelectedCategory = null;
        Name = string.Empty;
        Unit = string.Empty;
        IsEditing = false;
    }
}
