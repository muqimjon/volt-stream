namespace VoltStream.WPF.Sales.Views;

using ApiServices.Models.Responses;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.Utils;
using VoltStream.WPF.Sales.ViewModels;
using VoltStream.WPF.Turnovers.Models;

public partial class SaleEditPage : Page
{
    private readonly SaleEditViewModel viewModel;

    public SaleEditPage(IServiceProvider services, SaleResponse saleData)
    {
        InitializeComponent();
        viewModel = ActivatorUtilities.CreateInstance<SaleEditViewModel>(services, saleData);
        DataContext = viewModel;

        Loaded += Page_Loaded;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        RegisterFocusNavigation();
    }

    private void DataGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit || e.Row.Item is not SaleItemViewModel item || e.EditingElement is not TextBox box
            || DataGridExcelEditing.IsUnchanged(box))
            return;

        var column = e.Column;
        bool accepted = decimal.TryParse(NumericInput.Normalize(box.Text), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
            ? column == rollCountColumn ? viewModel.ApplyRollCount(item, value)
                : column == totalLengthColumn ? viewModel.ApplyTotalLength(item, value)
                : column == unitPriceColumn ? viewModel.ApplyUnitPrice(item, value)
                : viewModel.ApplyDiscountRate(item, value)
            : viewModel.Reject(column == rollCountColumn ? "Sales.RollCountRequired"
                : column == totalLengthColumn ? "Sales.TotalQuantityRequired"
                : column == unitPriceColumn ? "Sales.PriceRequired"
                : "Sales.DiscountPercentRange");

        if (accepted)
            return;

        e.Cancel = true;
        Dispatcher.BeginInvoke(() => dataGrid.CancelEdit(DataGridEditingUnit.Row), DispatcherPriority.Background);
    }

    private void RegisterFocusNavigation()
    {
        FocusNavigator.RegisterElements(
            [
                calendar.calendar,
                cbxCustomer,
                cbxCurrency,
                txtSaleTotalAmount,
                txtSaleTotalDiscount,
                chkIsDiscountApplied,
                txtSaleAmount,
                txtSaleDescription,
                cbxCategory,
                cbxProduct,
                cbxLengthPerRoll,
                txtRollCount,
                txtTotalLength,
                txtUnitPrice,
                txtTotalAmount,
                txtDiscountRate,
                txtDiscountAmount,
                txtFinalAmount,
                btnAdd
            ]);

        FocusNavigator.SetFocusRedirect(btnAdd, cbxCategory);
    }
}