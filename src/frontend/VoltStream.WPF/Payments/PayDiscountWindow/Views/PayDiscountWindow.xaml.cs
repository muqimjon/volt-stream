namespace VoltStream.WPF.Payments.PayDiscountWindow.Views;

using System.Windows;

public partial class PayDiscountWindow : Window
{
    public PayDiscountWindow(long id, string name, decimal bonus)
    {
        InitializeComponent();
        txtCustomer.Text = name;
        AmauntDiscount.Text = bonus.ToString("N2");
        inCash.GotFocus += InCash_GotFocus;
        reCalculation.GotFocus += Recalculation_GotFocus;
        DiscountSum.Focus();
    }
    public dynamic? ResultOfDiscount { get; private set; }
    private void InCash_GotFocus(object sender, RoutedEventArgs e)
    {
        inCash.IsChecked = true;
    }

    private void Recalculation_GotFocus(object sender, RoutedEventArgs e)
    {
        reCalculation.IsChecked = true;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        DiscountSum.Text = AmauntDiscount.Text;
        reCalculation.Focus();
    }
    private void SaveDiscount_Click(object sender, RoutedEventArgs e)
    {
        ResultOfDiscount = new
        {
            discountCash = inCash.IsChecked == true ? true : false,
            discountSum = decimal.TryParse(DiscountSum.Text, out var d) ? d : 0,
            discountInfo = txtDescription.Text
        };
        DialogResult = true;
        Close();

    }
}
