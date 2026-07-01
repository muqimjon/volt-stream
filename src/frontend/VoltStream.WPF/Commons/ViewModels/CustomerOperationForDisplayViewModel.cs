namespace VoltStream.WPF.Commons.ViewModels;
using ApiServices.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using VoltStream.WPF.Sales.ViewModels;
public partial class CustomerOperationForDisplayViewModel : ObservableObject
{
    public long Id { get; set; }
    public long? CustomerId { get; set; }
    public long AccountId { get; set; }
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private DateTime date;
    [ObservableProperty] private string customer = string.Empty;
    [ObservableProperty] private decimal debit;
    [ObservableProperty] private decimal credit;
    [ObservableProperty] private string? description;
    [ObservableProperty] private TextBlock? formattedTextBlock;
    [ObservableProperty] private OperationType operationType;
    [ObservableProperty] private AccountViewModel account = new();
    public bool CanEdit => OperationType == OperationType.Sale;
    public bool IsEditable { get; set; }
    [ObservableProperty] private Sale? sale;
    [ObservableProperty] private PaymentViewModel? payment;
    public string FormattedDescription
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Description))
                return string.Empty;
            return string.Join("\n",
                Description.Split(';')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x)));
        }
    }
    partial void OnDescriptionChanged(string? oldValue, string? newValue)
    {
        FormattedTextBlock = CreateFormattedTextBlock();
    }
    private TextBlock CreateFormattedTextBlock()
    {
        var textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(5, 3, 5, 3),
            TextAlignment = TextAlignment.Left
        };

        if (string.IsNullOrWhiteSpace(Description))
            return textBlock;

        var lines = Description.Split([';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToArray();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            int colon = line.IndexOf(':');
            if (colon >= 0)
            {
                textBlock.Inlines.Add(new Run(line[..(colon + 1)]) { FontWeight = FontWeights.Bold });
                textBlock.Inlines.Add(new Run(line[(colon + 1)..]));
            }
            else
                textBlock.Inlines.Add(new Run(line));

            if (i < lines.Length - 1)
                textBlock.Inlines.Add(new LineBreak());
        }

        return textBlock;
    }
    partial void OnOperationTypeChanged(OperationType oldValue, OperationType newValue)
    {
        IsEditable = newValue == OperationType.Sale;
    }
}