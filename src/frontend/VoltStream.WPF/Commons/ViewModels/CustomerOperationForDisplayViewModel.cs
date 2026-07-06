namespace VoltStream.WPF.Commons.ViewModels;
using ApiServices.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
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
    [ObservableProperty] private FrameworkElement? formattedTextBlock;
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
    private FrameworkElement CreateFormattedTextBlock()
    {
        var panel = new StackPanel { Margin = new Thickness(5, 3, 5, 3) };
        if (string.IsNullOrWhiteSpace(Description))
            return panel;

        Grid? items = null;
        foreach (var raw in Description.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                items = null;
                continue;
            }

            if (line.All(c => c == '-'))
            {
                items = null;
                panel.Children.Add(BuildSeparator());
                continue;
            }

            if (TryParseItem(line, out var name, out var length, out var price, out var sum, out var disc))
            {
                items ??= AppendItemsGrid(panel);
                AddItemRow(items, name, length, price, sum, disc);
            }
            else
            {
                items = null;
                var t = line.TrimStart();
                if (t.StartsWith("Jami", StringComparison.OrdinalIgnoreCase) || t.StartsWith("Chegirma", StringComparison.OrdinalIgnoreCase))
                    panel.Children.Add(BuildTotalLine(line));
                else
                    panel.Children.Add(BuildTextLine(line));
            }
        }
        return panel;
    }

    private static Grid AppendItemsGrid(StackPanel panel)
    {
        var g = new Grid { Margin = new Thickness(0, 2, 0, 3) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 4; i++)
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panel.Children.Add(g);
        return g;
    }

    private static void AddItemRow(Grid g, string name, string length, string price, string sum, string disc)
    {
        int r = g.RowDefinitions.Count;
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        bool hasDisc = disc.Length > 0;

        g.Children.Add(ItemCell(name, 0, r, TextAlignment.Left, null, new Thickness(0, 1, 10, 1), true));
        g.Children.Add(ItemCell(length, 1, r, TextAlignment.Right, "TextSecondary", new Thickness(0, 1, 10, 1), false));
        g.Children.Add(ItemCell("× " + price, 2, r, TextAlignment.Right, "TextSecondary", new Thickness(0, 1, 10, 1), false));
        g.Children.Add(ItemCell("= " + sum, 3, r, TextAlignment.Right, null, new Thickness(0, 1, hasDisc ? 10 : 0, 1), false));
        if (hasDisc)
            g.Children.Add(ItemCell("(" + disc + ")", 4, r, TextAlignment.Right, "TextSecondary", new Thickness(0, 1, 0, 1), false));
    }

    private static TextBlock ItemCell(string text, int col, int row, TextAlignment align, string? foregroundKey, Thickness margin, bool wrap)
    {
        var tb = new TextBlock
        {
            Text = text,
            TextAlignment = align,
            Margin = margin,
            TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap
        };
        if (foregroundKey is not null)
            tb.SetResourceReference(TextBlock.ForegroundProperty, foregroundKey);
        Grid.SetColumn(tb, col);
        Grid.SetRow(tb, row);
        return tb;
    }

    private static Border BuildSeparator()
    {
        var border = new Border
        {
            Height = 1,
            Width = 40,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 4)
        };
        border.SetResourceReference(Border.BackgroundProperty, "BorderStrong");
        return border;
    }

    private static TextBlock BuildTextLine(string line) => new()
    {
        Text = line,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 1, 0, 1)
    };

    private static FrameworkElement BuildTotalLine(string line)
    {
        int c = line.IndexOf(':');
        if (c < 0)
            return new TextBlock { Text = line, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 1) };

        var value = line[(c + 1)..].Trim();
        int p = value.LastIndexOf('(');
        if (p > 0 && value.EndsWith(")"))
            value = $"{value[p..]} {value[..p].Trim()}";

        var g = new Grid { Margin = new Thickness(0, 1, 0, 1) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var lbl = new TextBlock { Text = line[..c].Trim(), HorizontalAlignment = HorizontalAlignment.Left };
        var val = new TextBlock { Text = value, HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetColumn(val, 1);
        g.Children.Add(lbl);
        g.Children.Add(val);
        return g;
    }

    private static string NormalizeDiscount(string disc)
    {
        int pct = disc.IndexOf('%');
        if (pct > 0 && decimal.TryParse(disc[..pct].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
            return $"{rate:0.##}{disc[pct..]}";
        return disc;
    }

    private static bool TryParseItem(string line, out string name, out string length, out string price, out string sum, out string disc)
    {
        name = length = price = sum = disc = string.Empty;
        var body = line.TrimEnd(';', ' ');

        int br = body.IndexOf("[ch:", StringComparison.OrdinalIgnoreCase);
        if (br >= 0)
        {
            int end = body.IndexOf(']', br);
            disc = (end > br ? body[(br + 4)..end] : body[(br + 4)..]).Trim();
            disc = NormalizeDiscount(disc);
            body = body[..br].Trim();
        }

        int eq = body.LastIndexOf('=');
        if (eq < 0)
            return false;
        sum = body[(eq + 1)..].Trim();

        var left = body[..eq].Trim();
        int x = left.LastIndexOf(" x ", StringComparison.Ordinal);
        if (x < 0)
            return false;
        price = left[(x + 3)..].Trim();

        var nameLen = left[..x].Trim();
        int dash = nameLen.LastIndexOf(" - ", StringComparison.Ordinal);
        if (dash < 0)
            return false;
        length = nameLen[(dash + 3)..].Trim();
        name = nameLen[..dash].Trim();
        return name.Length > 0;
    }
    partial void OnOperationTypeChanged(OperationType oldValue, OperationType newValue)
    {
        IsEditable = newValue == OperationType.Sale;
    }
}