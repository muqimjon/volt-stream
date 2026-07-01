namespace VoltStream.WPF.Sales.ViewModels;

public class SaleSession
{
    public Sale Current { get; private set; } = new();

    public void Reset() => Current = new Sale();
}
