namespace VoltStream.WPF.Sales.Views;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.Utils;
using VoltStream.WPF.Customer;
using VoltStream.WPF.Sales.ViewModels;

public partial class SalesPage : Page
{
    private readonly IServiceProvider services;
    private readonly ICategoriesApi categoriesApi;
    private readonly IProductsApi productsApi;
    private readonly IWarehouseStocksApi warehouseItemsApi;
    private readonly ICustomersApi customersApi;
    private readonly ICurrenciesApi currenciesApi;
    private readonly ISaleApi salesApi;

    private readonly SaleSession saleSession;
    public Sale sale;

    public SalesPage(IServiceProvider services)
    {
        InitializeComponent();
        this.services = services;
        saleSession = services.GetRequiredService<SaleSession>();
        sale = saleSession.Current;
        DataContext = sale;
        categoriesApi = services.GetRequiredService<ICategoriesApi>();
        productsApi = services.GetRequiredService<IProductsApi>();
        warehouseItemsApi = services.GetRequiredService<IWarehouseStocksApi>();
        customersApi = services.GetRequiredService<ICustomersApi>();
        salesApi = services.GetRequiredService<ISaleApi>();
        currenciesApi = services.GetRequiredService<ICurrenciesApi>();

        CustomerName.GotFocus += CustomerName_GotFocus;

        CustomerName.PreviewLostKeyboardFocus += CustomerName_PreviewLostKeyboardFocus;
        CustomerName.LostFocus += CustomerName_LostFocus;

        cbxCategoryName.GotFocus += CbxCategoryName_GotFocus;
        cbxCategoryName.PreviewLostKeyboardFocus += CbxCategoryName_PreviewLostKeyboardFocus;

        cbxProductName.GotFocus += CbxProductName_GotFocus;
        cbxProductName.SelectionChanged += CbxProductName_SelectionChanged;
        cbxProductName.PreviewLostKeyboardFocus += CbxProductName_PreviewLostKeyboardFocus;
        cbxProductName.LostFocus += CbxProductName_LostFocus;

        cbxPerRollCount.SelectionChanged += CbxPerRollCount_SelectionChanged;
        cbxPerRollCount.PreviewLostKeyboardFocus += CbxPerRollCount_PreviewLostKeyboardFocus;

        txtRollCount.LostFocus += (s, e) => CalcFinalSumProduct(s);
        txtRollCount.PreviewLostKeyboardFocus += TxtRollCount_PreviewLostKeyboardFocus;
        txtQuantity.PreviewLostKeyboardFocus += TxtQuantity_PreviewLostKeyboardFocus;
        txtPrice.LostFocus += (s, e) => CalcFinalSumProduct(s);
        txtSum.LostFocus += TxtSum_LostFocus;
        txtPerDiscount.PreviewLostKeyboardFocus += TxtPerDiscount_PreviewLostKeyboardFocus;
        txtDiscount.PreviewLostKeyboardFocus += TxtDiscount_PreviewLostKeyboardFocus;
        txtFinalSumProduct.PreviewLostKeyboardFocus += TxtFinalSumProduct_PreviewLostKeyboardFocus;


        CurrencyType.SelectionChanged += CurrencyType_SelectionChanged;

    }

    private void CurrencyType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        sale.CurrencyId = CurrencyType.SelectedValue is not null ? (long)CurrencyType.SelectedValue : 0;
    }

    private void TxtRollCount_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (txtRollCount.Text.Length > 0)
        {
            if ((decimal.TryParse(txtRollCount.Text, out decimal value) ? value : 0) > sale.WarehouseCountRoll)
            {
                if (MessageBox.Show(string.Format(TranslationSource.T("Sales.RollStockWarning"), cbxProductName.Text, sale.WarehouseCountRoll),
                    TranslationSource.T("Sales.SaleCaption"), MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                {
                    e.Handled = true;
                    txtRollCount.Text = null;
                }
            }
        }
    }

    private async void CustomerName_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {

        bool accept = ComboBoxHelper.BeforeUpdate(sender, e, TranslationSource.T("Sales.Buyer"), true);
        if (!accept) return;

        var win = new CustomerWindow(CustomerName.Text)
        {
            Owner = Window.GetWindow(this),
        };

        if (win.ShowDialog() == true)
        {
            var customer = win.Result;
            CustomerRequest newCustomer = new()
            {
                Name = customer!.name,
                Phone = customer.phone,
                Address = customer.address,
                Description = customer.description,
                Accounts = [new()
                    {
                        OpeningBalance = customer.beginningSum,
                        Balance = customer.beginningSum,
                    }]
            };

            var response = await customersApi.CreateAsync(newCustomer).Handle();
            if (response.IsSuccess)
            {
                await LoadCustomerByIdAsync(response.Data);
                CustomerName.Text = newCustomer.Name;
                sale.CustomerId = response.Data;
                await LoadCurrencyAsync();
            }
            else sale.Error = response.Message ?? TranslationSource.T("Sales.CustomerCreateError");
        }
        else { e.Handled = true; }
    }

    private async void CustomerName_LostFocus(object sender, RoutedEventArgs e)
    {
        if (CustomerName.SelectedValue is not null)
        {
            sale.CustomerId = (long)CustomerName.SelectedValue;
        }
        else
        {
            beginBalans.Clear();
            lastBalans.Text = null;
            tel.Text = null;
            return;
        }
        await LoadCustomerByIdAsync(sale.CustomerId);
    }

    private async Task LoadCustomerByIdAsync(long? customerId)
    {
        if (!customerId.HasValue || customerId.Value == 0)
            return;


        FilteringRequest request = new()
        {
            Filters = new()
            {
                ["id"] = [customerId.Value.ToString()],
                ["accounts"] = ["include:currency"]
            }
        };

        var response = await customersApi.FilterAsync(request).Handle();
        if (response.IsSuccess)
        {
            var customer = response.Data!.First();

            beginBalans.Text = GetAccountsSumInUzsString(customer);
            tel.Text = customer.Phone;
            CalcSaleSum();
        }
        else sale.Error = response.Message ?? TranslationSource.T("Sales.CustomerInfoLoadError");

    }


    private static string GetAccountsSumInUzsString(CustomerResponse customer)
    {
        if (customer?.Accounts == null) return "0";

        decimal totalUzs = 0m;
        foreach (var acc in customer.Accounts)
        {
            if (acc == null) continue;
            var rate = acc.Currency?.ExchangeRate ?? 1m;
            if (rate == 0m) rate = 1m;
            totalUzs += acc.Balance * rate;
        }

        var rounded = Math.Round(totalUzs, 0, MidpointRounding.AwayFromZero);
        return rounded.ToString("F0");
    }


    private async void CustomerName_GotFocus(object sender, RoutedEventArgs e)
    {
        await LoadCustomerNameAsync();
    }
    private async Task LoadCustomerNameAsync()
    {
        var selectedValue = CustomerName.SelectedValue;
        var response = await customersApi.GetAllAsync().Handle();

        if (response.IsSuccess)
        {
            List<CustomerResponse> customers = response.Data!;
            CustomerName.ItemsSource = customers;
            CustomerName.DisplayMemberPath = "Name";
            CustomerName.SelectedValuePath = "Id";

            if (selectedValue is not null)
                CustomerName.SelectedValue = selectedValue;
        }
        else sale.Error = response.Message ?? TranslationSource.T("Sales.CustomersLoadError");
    }

    private void TxtFinalSumProduct_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (decimal.TryParse(txtFinalSumProduct.Text, out decimal finalSum) &&
            decimal.TryParse(txtSum.Text, out decimal sum) && sum != 0)
        {
            if (finalSum > sum)
            {
                sale.Error = TranslationSource.T("Sales.DiscountedSumExceedsTotal");
                txtPerDiscount.Text = null;
                txtDiscount.Text = null;
                txtFinalSumProduct.Text = null;
                e.Handled = true;
                return;
            }
            decimal discount = sum - finalSum;
            decimal perDiscount = (discount / sum * 100);
            txtDiscount.Text = discount.ToString();
            txtPerDiscount.Text = perDiscount.ToString();
        }
        else
        {
            txtFinalSumProduct.Text = null;
            txtPerDiscount.Text = "0";
            CalcFinalSumProduct(sender);
        }
    }

    private void TxtDiscount_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (decimal.TryParse(txtDiscount.Text, out decimal discount) &&
            decimal.TryParse(txtSum.Text, out decimal sum) && sum != 0)
        {
            if (discount > sum)
            {
                sale.Error = TranslationSource.T("Sales.DiscountExceedsTotal");
                txtPerDiscount.Text = null;
                txtDiscount.Text = null;
                txtFinalSumProduct.Text = null;
                e.Handled = true;
                return;
            }
            decimal perDiscount = (discount / sum * 100);
            txtFinalSumProduct.Text = (sum - discount).ToString();
            txtPerDiscount.Text = perDiscount.ToString();
        }
        else
        {
            txtDiscount.Text = "0";
            txtPerDiscount.Text = "0";
            CalcFinalSumProduct(sender);
        }
    }


    private void TxtPerDiscount_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (decimal.TryParse(txtPerDiscount.Text, out decimal perDiscount) &&
            perDiscount != 0)
        {
            if (perDiscount >= 100)
            {
                sale.Error = TranslationSource.T("Sales.DiscountOver100");
                txtPerDiscount.Text = null;
                txtDiscount.Text = null;
                txtFinalSumProduct.Text = null;
                e.Handled = true;
                return;
            }
            CalcFinalSumProduct(sender);
        }
        else
        {
            txtPerDiscount.Text = "0";
            CalcFinalSumProduct(sender);
        }
    }

    private void TxtSum_LostFocus(object sender, RoutedEventArgs e)
    {
        if (decimal.TryParse(txtSum.Text, out decimal sum) &&
        decimal.TryParse(txtQuantity.Text, out decimal quantity) && quantity != 0)
        {
            decimal price = sum / quantity;
            txtPrice.Text = price.ToString();
            CalcFinalSumProduct(sender);
        }
        else
        {
            txtSum.Text = null;
            txtFinalSumProduct.Text = null;
        }
    }


    private void TxtQuantity_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (decimal.TryParse(txtQuantity.Text, out decimal quantity) &&
            decimal.TryParse(cbxPerRollCount.Text, out decimal perRollCount) &&
            perRollCount != 0)
        {
            if ((decimal.TryParse(txtQuantity.Text, out decimal value) ? value : 0) > sale.WarehouseQuantity)
            {
                if (MessageBox.Show(string.Format(TranslationSource.T("Sales.MeterStockWarning"), cbxProductName.Text, cbxPerRollCount.Text, sale.WarehouseQuantity),
                    TranslationSource.T("Sales.SaleCaption"), MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                {
                    e.Handled = true;
                    txtQuantity.Text = null;
                    return;
                }
            }
            if (quantity % perRollCount != 0)
            {
                int intRollCount = (int)(quantity / perRollCount);
                decimal _q = (decimal)intRollCount * perRollCount;
                decimal _quantity = quantity - _q;
                decimal q_quantity = perRollCount - _quantity;
                sale.NewQuantity = q_quantity;
                if (MessageBox.Show(string.Format(TranslationSource.T("Sales.RollCutWarning"), cbxProductName.Text, cbxPerRollCount.Text, quantity, _quantity, q_quantity),
                    TranslationSource.T("Sales.SaleCaption"), MessageBoxButton.YesNo, MessageBoxImage.Question,
                    MessageBoxResult.No) == MessageBoxResult.No)
                {
                    e.Handled = true;
                    txtQuantity.Text = _q.ToString();
                    sale.NewQuantity = 0;
                    txtQuantity.SelectAll();
                    return;
                }
            }

            decimal rollCount = Math.Ceiling(quantity / perRollCount);
            txtRollCount.Text = rollCount.ToString();
            CalcFinalSumProduct(sender);
        }
        else
        {
            txtQuantity.Text = null;
            txtSum.Text = null;
            txtFinalSumProduct.Text = null;
        }
    }

    private void CalcFinalSumProduct(object sender)
    {
        if (sender == cbxPerRollCount || sender == txtRollCount)
        {
            if (decimal.TryParse(txtRollCount.Text, out decimal rollCount) &&
            decimal.TryParse(cbxPerRollCount.Text, out decimal perRollCount))
            {
                decimal totalQuantity = rollCount * perRollCount;
                txtQuantity.Text = totalQuantity.ToString();
            }
            else
            {
                txtQuantity.Text = null;
                txtSum.Text = null;
                txtFinalSumProduct.Text = null;
            }
        }
        if (decimal.TryParse(txtPrice.Text, out decimal price) &&
            decimal.TryParse(txtQuantity.Text, out decimal quantity) &&
            decimal.TryParse(txtPerDiscount.Text, out decimal discountPercent))
        {
            decimal totalPrice = price * quantity;
            decimal discountAmount = totalPrice * (discountPercent / 100);
            decimal finalPrice = totalPrice - discountAmount;
            txtSum.Text = totalPrice.ToString();
            txtDiscount.Text = discountAmount.ToString();
            txtFinalSumProduct.Text = finalPrice.ToString();
        }
        else
        {
            txtSum.Text = null;
            txtDiscount.Text = null;
            txtFinalSumProduct.Text = null;
        }
    }

    private void CbxPerRollCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cbxPerRollCount.SelectedItem is WarehouseStockResponse selectedWarehouseItem)
        {
            txtPrice.Text = selectedWarehouseItem.UnitPrice.ToString();
            txtPerDiscount.Text = selectedWarehouseItem.DiscountRate.ToString();
            sale.WarehouseCountRoll = selectedWarehouseItem.RollCount;
            sale.WarehouseQuantity = selectedWarehouseItem.TotalLength;
            CalcFinalSumProduct(sender);
        }
    }

    private async void CbxCategoryName_GotFocus(object sender, RoutedEventArgs e)
    {
        await LoadCategoryAsync();
    }
    private void CbxCategoryName_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        _ = ComboBoxHelper.BeforeUpdate(sender, e, TranslationSource.T("Sales.ProductType"));
    }

    private async void CbxProductName_GotFocus(object sender, RoutedEventArgs e)
    {
        long? categoryId = null;
        if (cbxCategoryName.SelectedValue is not null)
            categoryId = (long)cbxCategoryName.SelectedValue;

        await LoadProductAsync(categoryId);
    }
    private void CbxProductName_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cbxProductName.SelectedItem is ProductResponse selectedProduct)
        {
            cbxCategoryName.SelectedValue = selectedProduct.Category.Id;
            sale.CategoryId = selectedProduct.Category.Id;
            sale.CategoryName = (cbxCategoryName.ItemsSource as IEnumerable<CategoryResponse>)?
                .FirstOrDefault(c => c.Id == selectedProduct.Category.Id)?.Name ?? string.Empty;
            sale.ProductId = selectedProduct.Id;
        }
    }

    private void CbxProductName_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ComboBoxHelper.BeforeUpdate(sender, e, TranslationSource.T("Sales.Product"));
    }

    private async void CbxProductName_LostFocus(object sender, RoutedEventArgs e)
    {
        long? productId = null;
        if (cbxProductName.SelectedValue is not null)
            productId = (long)cbxProductName.SelectedValue;

        await LoadWarehouseItemsAsync(productId);
    }

    private void CbxPerRollCount_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ComboBoxHelper.BeforeUpdate(sender, e, TranslationSource.T("Sales.RollLength"));
    }

    private async Task LoadCategoryAsync()
    {
        var selectedValue = cbxCategoryName.SelectedValue;

        var response = await categoriesApi.GetAllAsync().Handle();
        if (response.IsSuccess)
        {
            List<CategoryResponse> categories = response.Data!;
            cbxCategoryName.ItemsSource = categories;
            cbxCategoryName.DisplayMemberPath = "Name";
            cbxCategoryName.SelectedValuePath = "Id";

            if (selectedValue is not null)
                cbxCategoryName.SelectedValue = selectedValue;
        }
        else sale.Error = response.Message ?? TranslationSource.T("Sales.CategoriesLoadError");
    }

    private async Task LoadProductAsync(long? categoryId)
    {
        var selectedValue = cbxProductName.SelectedValue;
        Response<List<ProductResponse>> response;

        FilteringRequest request = new() { Filters = new() { ["category"] = ["include"] } };
        if (categoryId.HasValue && categoryId.Value != 0)
            request.Filters["CategoryId"] = [categoryId.Value.ToString()];

        response = await productsApi.Filter(request).Handle();

        if (response.IsSuccess)
        {
            var products = response.Data;
            cbxProductName.ItemsSource = products;
            cbxProductName.DisplayMemberPath = "Name";
            cbxProductName.SelectedValuePath = "Id";

            if (selectedValue is not null)
                cbxProductName.SelectedValue = selectedValue;
        }
        else
            sale.Error = response.Message ?? TranslationSource.T("Sales.ProductsLoadError");
    }

    private async Task LoadWarehouseItemsAsync(long? productId)
    {
        var selectedValue = cbxPerRollCount.SelectedValue;
        Response<List<WarehouseStockResponse>> response;
        if (!productId.HasValue || productId.Value == 0)
        {
            cbxPerRollCount.ItemsSource = null;
            return;
        }

        response = await warehouseItemsApi.GetProductDetailsFromWarehouseAsync(productId.Value).Handle();
        if (response.IsSuccess)
        {
            var warehouseItems = response.Data;
            cbxPerRollCount.ItemsSource = warehouseItems;
            cbxPerRollCount.DisplayMemberPath = "LengthPerRoll";
            cbxPerRollCount.SelectedValuePath = "LengthPerRoll";
            if (selectedValue is not null)
                cbxPerRollCount.SelectedValue = selectedValue;
        }
        else sale.Error = response.Message ?? TranslationSource.T("Sales.WarehouseLoadError");
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsSaleItemCompleted())
            return;

        SaleItem saleItem = new()
        {
            CategoryId = cbxCategoryName.SelectedIndex,
            CategoryName = cbxCategoryName.Text,
            ProductId = (long)cbxProductName.SelectedValue,
            ProductName = cbxProductName.Text,
            PerRollCount = decimal.TryParse(cbxPerRollCount.Text, out decimal perRollCount) ? perRollCount : 0,
            RollCount = int.TryParse(txtRollCount.Text, out int rollCount) ? rollCount : 0,
            WarehouseCountRoll = sale.WarehouseCountRoll,
            Quantity = decimal.TryParse(txtQuantity.Text, out decimal quantity) ? quantity : 0,
            NewQuantity = sale.NewQuantity,
            WarehouseQuantity = sale.WarehouseQuantity,
            Price = decimal.TryParse(txtPrice.Text, out decimal price) ? price : 0,
            Sum = decimal.TryParse(txtSum.Text, out decimal sum) ? sum : 0,
            PerDiscount = decimal.TryParse(txtPerDiscount.Text, out decimal perDiscount) ? perDiscount : 0,
            Discount = decimal.TryParse(txtDiscount.Text, out decimal discount) ? discount : 0,
            FinalSumProduct = decimal.TryParse(txtFinalSumProduct.Text, out decimal finalSumProduct) ? finalSumProduct : 0
        };

        sale.SaleItems.Insert(0, saleItem);

        CalcSaleSum();
        cbxCategoryName.SelectedValue = null;
        cbxProductName.SelectedValue = null;
        cbxPerRollCount.SelectedValue = null;
        txtRollCount.Clear();
        txtQuantity.Clear();
        txtPrice.Clear();
        txtSum.Clear();
        txtPerDiscount.Clear();
        txtDiscount.Clear();
        txtFinalSumProduct.Clear();
        cbxCategoryName.Focus();
        sale.NewQuantity = 0;
        sale.WarehouseCountRoll = 0;
        sale.WarehouseQuantity = 0;
    }

    private bool IsSaleItemCompleted()
    {
        bool isSuccess = true;
        decimal perDiscount = 0;

        if (cbxCategoryName.SelectedValue == null)
        {
            MessageBox.Show(TranslationSource.T("Sales.CategoryNotSelected"), TranslationSource.T("Sales.ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            cbxCategoryName.Focus();
            isSuccess = false;
        }

        else if (cbxProductName.SelectedValue == null)
        {
            cbxProductName.Focus();
            sale.Warning = TranslationSource.T("Sales.ProductNotSelected");
            isSuccess = false;
        }

        else if (cbxPerRollCount.SelectedValue == null)
        {
            cbxPerRollCount.Focus();
            sale.Warning = TranslationSource.T("Sales.PerRollNotSelected");
            isSuccess = false;
        }

        else if (!int.TryParse(txtRollCount.Text, out int rollCount) || rollCount <= 0)
        {
            txtRollCount.Focus();
            sale.Warning = TranslationSource.T("Sales.RollCountRequired");
            isSuccess = false;
        }

        else if (!decimal.TryParse(txtQuantity.Text, out decimal quantity) || quantity <= 0)
        {
            txtQuantity.Focus();
            sale.Warning = TranslationSource.T("Sales.TotalQuantityRequired");
            isSuccess = false;
        }

        else if (!decimal.TryParse(txtPrice.Text, out decimal price) || price <= 0)
        {
            txtPrice.Focus();
            sale.Warning = TranslationSource.T("Sales.PriceRequired");
            isSuccess = false;
        }

        else if (!decimal.TryParse(txtSum.Text, out decimal sum) || sum <= 0)
        {
            sale.Warning = TranslationSource.T("Sales.TotalSumRequired");
            txtSum.Focus();
            isSuccess = false;
        }

        else if (!decimal.TryParse(txtFinalSumProduct.Text, out decimal finalSumProduct) || finalSumProduct <= 0)
        {
            sale.Warning = TranslationSource.T("Sales.GrandTotalRequired");
            txtFinalSumProduct.Focus();
            isSuccess = false;
        }
        else if (!string.IsNullOrWhiteSpace(txtPerDiscount.Text) && !decimal.TryParse(txtPerDiscount.Text, out perDiscount))
        {
            sale.Warning = TranslationSource.T("Sales.DiscountPercentInvalid");
            txtPerDiscount.Focus();
            isSuccess = false;
        }

        else if (!string.IsNullOrWhiteSpace(txtDiscount.Text) && !decimal.TryParse(txtDiscount.Text, out decimal discount))
        {
            sale.Warning = TranslationSource.T("Sales.DiscountAmountInvalid");
            txtDiscount.Focus();
            isSuccess = false;
        }

        else if (perDiscount < 0 || perDiscount > 100)
        {
            sale.Warning = TranslationSource.T("Sales.DiscountPercentRange");
            txtPerDiscount.Focus();
            isSuccess = false;
        }

        return isSuccess;
    }

    private void CalcSaleSum()
    {
        decimal finalSum = 0;
        decimal totalSum = 0;
        decimal totalDiscount = 0;
        if (sale.SaleItems.Count > 0)
        {
            sale.FinalSum = sale.SaleItems.Sum(s => s.Sum);
            sale.TotalSum = sale.SaleItems.Sum(s => s.Sum);
            sale.TotalDiscount = sale.SaleItems.Sum(d => d.Discount);
            finalSum = sale.FinalSum!.Value;
            totalSum = sale.TotalSum!.Value;
            totalDiscount = sale.TotalDiscount!.Value;
            if (sale.IsDiscountApplied)
            {
                finalSum = totalSum - totalDiscount;
                sale.FinalSum = finalSum;
            }
        }
        decimal beginSum = decimal.TryParse(beginBalans.Text, out decimal value) ? value : 0;
        decimal endSum = beginSum - finalSum;
        lastBalans.Text = endSum.ToString();

    }

    private void CheckedDiscount_Click(object sender, RoutedEventArgs e)
    {
        CalcSaleSum();

    }

    private async void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        saleSession.Reset();
        sale = saleSession.Current;
        await ClearUI();
    }

    private async void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        if (saleDate.SelectedDate is null)
        {
            sale.Warning = TranslationSource.T("Sales.DateNotSelected");
            saleDate.Focus();
            return;
        }

        if (saleDate.SelectedDate.Value.Date == DateTime.Today)
            sale.OperationDate = DateTime.Now;
        else
            sale.OperationDate = saleDate.SelectedDate.Value;

        if (sale.SaleItems.Count == 0)
        {
            sale.Warning = TranslationSource.T("Sales.NoProductsAdded");
            return;
        }

        if (!Confirm(string.Format(TranslationSource.T("Sales.SubmitConfirm"), sale.OperationDate, sale.SaleItems.Select(si => si.ProductId).Distinct().Count())))
            return;

        var saleRequest = new SaleRequest
        {
            Date = sale.OperationDate,
            CustomerId = sale.CustomerId,
            Amount = sale.FinalSum ?? 0,
            Discount = sale.TotalDiscount ?? 0,
            Description = sale.Description,
            CurrencyId = sale.CurrencyId,
            Length = (decimal)sale.SaleItems.Sum(si => si.Quantity)!,
            RollCount = (int)sale.SaleItems.Sum(si => si.RollCount)!,
            IsDiscountApplied = sale.IsDiscountApplied,
            Items = [.. sale.SaleItems.Select(i => new SaleItemRequest
            {
                ProductId = i.ProductId,
                RollCount = i.RollCount ?? 0,
                LengthPerRoll = i.PerRollCount ?? 0,
                TotalLength = i.Quantity ?? 0,
                UnitPrice = i.Price ?? 0,
                TotalAmount = i.Sum ?? 0,
                DiscountRate = i.PerDiscount ?? 0,
                DiscountAmount = i.Discount ?? 0,
                FinalAmount = i.FinalSumProduct ?? 0
            })]
        };

        var s = sale.CurrencyId;

        var response = await salesApi.CreateAsync(saleRequest)
            .Handle(isLoading => sale.IsLoading = isLoading);

        if (response.IsSuccess)
        {
            sale.Success = TranslationSource.T("Sales.SaveSuccess");
            saleSession.Reset();
            sale = saleSession.Current;
            await ClearUI();
            CustomerName.Focus();
        }
        else
        {
            sale.Error = $"{TranslationSource.T("Sales.ServerError")}: {response.StatusCode}";
        }
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadCategoryAsync();
        await LoadCurrencyAsync();
        RegisterFocusNavigation();
    }

    private void RegisterFocusNavigation()
    {
        FocusNavigator.RegisterElements([
            saleDate.TextBox,
            CustomerName,
            checkedDiscount,
            noteTextBox,
            cbxCategoryName,
            cbxProductName,
            cbxPerRollCount,
            txtRollCount,
            txtQuantity,
            txtPrice,
            txtSum,
            txtPerDiscount,
            txtDiscount,
            txtFinalSumProduct,
            addButton
        ]);
        
        FocusNavigator.SetFocusRedirect(addButton, cbxCategoryName);
    }

    private async Task LoadCurrencyAsync()
    {
        var response = await currenciesApi.Filter(new FilteringRequest { Filters = new() }).Handle();

        if (response.IsSuccess && response.Data is { Count: > 0 } currencies)
        {
            CurrencyType.DisplayMemberPath = "Name";
            CurrencyType.SelectedValuePath = "Id";
            CurrencyType.ItemsSource = currencies;
            var index = sale.CurrencyId > 0
                ? currencies.FindIndex(c => c.Id == sale.CurrencyId)
                : currencies.FindIndex(c => c.IsDefault);
            CurrencyType.SelectedIndex = index >= 0 ? index : 0;
        }
    }

    private async Task ClearUI()
    {
        CustomerName.Text = string.Empty;
        CustomerName.SelectedIndex = -1;

        cbxCategoryName.Text = string.Empty;
        cbxCategoryName.SelectedIndex = -1;

        cbxProductName.Text = string.Empty;
        cbxProductName.SelectedIndex = -1;

        cbxPerRollCount.Text = string.Empty;
        cbxPerRollCount.SelectedIndex = -1;

        txtRollCount.Text = string.Empty;
        txtQuantity.Text = string.Empty;
        txtPrice.Text = string.Empty;
        txtSum.Text = string.Empty;
        txtPerDiscount.Text = string.Empty;
        txtDiscount.Text = string.Empty;
        txtFinalSumProduct.Text = string.Empty;
        txtTotalDiscount.Text = string.Empty;
        FinalSumm.Text = string.Empty;
        TotalSum.Text = string.Empty;
        noteTextBox.Text = string.Empty;
        beginBalans.Text = string.Empty;
        lastBalans.Text = string.Empty;
        tel.Text = string.Empty;

        checkedDiscount.IsChecked = false;
        saleDate.SelectedDate = DateTime.Now;

        sale.SaleItems.Clear();
        dataGrid.ItemsSource = sale.SaleItems;
        DataContext = sale;

        await LoadCurrencyAsync();
    }

    private void SupplyDate_LostFocus(object sender, RoutedEventArgs e)
    {
        string[] formats = ["dd.MM.yyyy", "dd-MM-yyyy", "dd/MM/yyyy"];

        if (DateTime.TryParseExact(saleDate.TextBox.Text, formats,
                                   CultureInfo.InvariantCulture,
                                   DateTimeStyles.None,
                                   out DateTime parsedDate))
        {
            saleDate.SelectedDate = parsedDate;
        }
        else
        {
            sale.Error = TranslationSource.T("Sales.InvalidDateFormat");
            saleDate.TextBox.Focus();
            return;
        }
    }

    private static bool Confirm(string message, MessageBoxImage image = MessageBoxImage.Question)
    {
        var result = MessageBox.Show(message, TranslationSource.T("Sales.ConfirmTitle"), MessageBoxButton.YesNo, image);
        return result == MessageBoxResult.Yes;
    }
}