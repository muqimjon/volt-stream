namespace VoltStream.WPF.Commons.ViewModels;

public class DebitorCreditorItemViewModel
{
    public string Customer { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Discount { get; set; }
    public decimal Debitor { get; set; }
    public decimal Creditor { get; set; }
}
