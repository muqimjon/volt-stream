namespace VoltStream.WPF.Commons.Services;

using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClosedXML.Excel;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

public enum ReportAlign { Left, Center, Right }

public sealed class ReportColumn<T>
{
    public string Header { get; init; } = string.Empty;
    public double Width { get; init; } = 80;
    public ReportAlign Align { get; init; } = ReportAlign.Left;
    public bool IsNumber { get; init; }
    public string? Format { get; init; }
    public required Func<T, object?> Value { get; init; }
}

public sealed class ReportTotalCell
{
    public required int Column { get; init; }
    public object? Value { get; init; }
    public string? Format { get; init; }
}

public sealed class ReportTotals
{
    public string Label { get; init; } = "JAMI:";
    public int LabelSpan { get; init; } = 1;
    public IReadOnlyList<ReportTotalCell> Cells { get; init; } = [];
}

public sealed class ReportDefinition<T>
{
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string SheetName { get; init; } = "Hisobot";
    public string FileName { get; init; } = "hisobot";
    public required IReadOnlyList<ReportColumn<T>> Columns { get; init; }
    public required IReadOnlyList<T> Rows { get; init; }
    public ReportTotals? Totals { get; init; }
}

public static class ReportService
{
    public const string Brand = "VoltStream";

    private const double PageWidth = 793.7;
    private const double PageHeight = 1122.5;
    private const double Margin = 30;
    private const double FooterZone = 45;
    private const double TotalsReserve = 32;

    private static readonly Brush Accent = Freeze(new SolidColorBrush(Color.FromRgb(0x4F, 0x46, 0xE5)));
    private static readonly Brush HeaderBg = Freeze(new SolidColorBrush(Color.FromRgb(0xEE, 0xF0, 0xFD)));
    private static readonly Brush Ink = Freeze(new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x37)));
    private static readonly Brush Muted = Freeze(new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)));
    private static readonly Brush RowLine = Freeze(new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB)));
    private static readonly Brush Zebra = Freeze(new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB)));

    public static bool ExportExcel<T>(ReportDefinition<T> def)
    {
        var dialog = new SaveFileDialog { Filter = "Excel fayllari (*.xlsx)|*.xlsx", FileName = $"{def.FileName}.xlsx" };
        if (dialog.ShowDialog() != true)
            return false;
        using var wb = BuildWorkbook(def);
        wb.SaveAs(dialog.FileName);
        return true;
    }

    public static bool Print<T>(ReportDefinition<T> def)
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true)
            return false;
        dlg.PrintDocument(BuildFixedDocument(def).DocumentPaginator, def.Title);
        return true;
    }

    public static void Preview<T>(ReportDefinition<T> def)
        => Preview(BuildFixedDocument(def), def.FileName, def.Title);

    public static void Preview(FixedDocument doc, string fileName, string title)
    {
        var viewer = new DocumentViewer { Document = doc, Margin = new Thickness(8) };
        string? savedPath = null;

        var saveButton = ToolButton("💾  Saqlash", "SuccessButton");
        saveButton.Click += (_, _) =>
        {
            try
            {
                var dialog = new SaveFileDialog { Filter = "PDF fayllari (*.pdf)|*.pdf", FileName = $"{fileName}.pdf" };
                if (dialog.ShowDialog() != true)
                    return;
                SavePdf(doc, dialog.FileName);
                savedPath = dialog.FileName;
                MessageBox.Show($"Hisobot saqlandi:\n{dialog.FileName}", "Saqlandi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Saqlashda xatolik: {ex.Message}", title, MessageBoxButton.OK, MessageBoxImage.Error); }
        };

        var openButton = ToolButton("📂  Ochish", "PrimaryButton");
        openButton.Click += (_, _) =>
        {
            try
            {
                savedPath ??= SaveToDocuments(doc, fileName);
                Process.Start(new ProcessStartInfo(savedPath) { UseShellExecute = true });
            }
            catch (Exception ex) { MessageBox.Show($"Ochishda xatolik: {ex.Message}", title, MessageBoxButton.OK, MessageBoxImage.Error); }
        };

        var shareButton = ToolButton("📤  Ulashish", "SecondaryButton");
        shareButton.Click += (_, _) =>
        {
            try
            {
                savedPath ??= SaveToDocuments(doc, fileName);
                ShareToTelegram(savedPath);
            }
            catch (Exception ex) { MessageBox.Show($"Ulashishda xatolik: {ex.Message}", title, MessageBoxButton.OK, MessageBoxImage.Error); }
        };

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(10, 8, 10, 8) };
        toolbar.Children.Add(saveButton);
        toolbar.Children.Add(openButton);
        toolbar.Children.Add(shareButton);

        var toolbarBar = new Border { Child = toolbar };
        toolbarBar.SetResourceReference(Border.BackgroundProperty, "SurfaceAlt");
        toolbarBar.SetResourceReference(Border.BorderBrushProperty, "Border");
        toolbarBar.BorderThickness = new Thickness(0, 0, 0, 1);

        viewer.Template = ThemedViewerTemplate();

        var layout = new DockPanel();
        DockPanel.SetDock(toolbarBar, Dock.Top);
        layout.Children.Add(toolbarBar);
        layout.Children.Add(viewer);

        var owner = Application.Current?.MainWindow;
        var window = new Window
        {
            Title = title,
            Width = 1000,
            Height = 820,
            Owner = owner is { IsLoaded: true } ? owner : null,
            WindowStartupLocation = owner is { IsLoaded: true } ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            ShowInTaskbar = false,
            Icon = owner?.Icon,
            Content = layout
        };
        window.SetResourceReference(Control.BackgroundProperty, "Surface");
        ApplyDarkTitleBar(window);
        window.ShowDialog();
    }

    private static ControlTemplate ThemedViewerTemplate()
    {
        const string xaml = """
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                         TargetType="{x:Type DocumentViewer}">
          <Border Background="{DynamicResource Surface}">
            <DockPanel>
              <Border DockPanel.Dock="Top" Padding="8,6"
                      Background="{DynamicResource SurfaceAlt}"
                      BorderBrush="{DynamicResource Border}" BorderThickness="0,0,0,1">
                <StackPanel Orientation="Horizontal">
                  <StackPanel.Resources>
                    <Style TargetType="Button">
                      <Setter Property="MinWidth" Value="34" />
                      <Setter Property="Height" Value="30" />
                      <Setter Property="Margin" Value="0,0,6,0" />
                      <Setter Property="Padding" Value="12,0" />
                      <Setter Property="Cursor" Value="Hand" />
                      <Setter Property="FontWeight" Value="SemiBold" />
                      <Setter Property="Foreground" Value="{DynamicResource TextPrimary}" />
                      <Setter Property="Background" Value="{DynamicResource Surface}" />
                      <Setter Property="Template">
                        <Setter.Value>
                          <ControlTemplate TargetType="Button">
                            <Border x:Name="b" Background="{TemplateBinding Background}"
                                    BorderBrush="{DynamicResource Border}" BorderThickness="1"
                                    CornerRadius="6" Padding="{TemplateBinding Padding}">
                              <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                            </Border>
                            <ControlTemplate.Triggers>
                              <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="b" Property="Background" Value="{DynamicResource BrandSubtle}" />
                                <Setter TargetName="b" Property="BorderBrush" Value="{DynamicResource Brand}" />
                              </Trigger>
                            </ControlTemplate.Triggers>
                          </ControlTemplate>
                        </Setter.Value>
                      </Setter>
                    </Style>
                  </StackPanel.Resources>
                  <Button Content="＋" FontSize="16" ToolTip="Kattalashtirish"
                          Command="{x:Static NavigationCommands.IncreaseZoom}"
                          CommandTarget="{Binding RelativeSource={RelativeSource TemplatedParent}}" />
                  <Button Content="－" FontSize="16" ToolTip="Kichiklashtirish"
                          Command="{x:Static NavigationCommands.DecreaseZoom}"
                          CommandTarget="{Binding RelativeSource={RelativeSource TemplatedParent}}" />
                  <Button Content="Sahifa eni" ToolTip="Sahifa eniga moslash"
                          Command="{x:Static DocumentViewer.FitToWidthCommand}"
                          CommandTarget="{Binding RelativeSource={RelativeSource TemplatedParent}}" />
                  <Border Width="1" Margin="6,2" Background="{DynamicResource Border}" />
                  <Button Content="Chop etish" ToolTip="Chop etish"
                          Command="{x:Static ApplicationCommands.Print}"
                          CommandTarget="{Binding RelativeSource={RelativeSource TemplatedParent}}" />
                </StackPanel>
              </Border>
              <ContentControl x:Name="PART_FindToolBarHost" DockPanel.Dock="Bottom" />
              <ScrollViewer x:Name="PART_ContentHost"
                            Background="{DynamicResource Canvas}"
                            CanContentScroll="True"
                            HorizontalScrollBarVisibility="Auto"
                            VerticalScrollBarVisibility="Auto" />
            </DockPanel>
          </Border>
        </ControlTemplate>
        """;
        return (ControlTemplate)XamlReader.Parse(xaml);
    }

    private static void ApplyDarkTitleBar(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                int useDark = ThemeManager.Current == AppTheme.Dark ? 1 : 0;
                if (DwmSetWindowAttribute(hwnd, 20, ref useDark, sizeof(int)) != 0)
                    DwmSetWindowAttribute(hwnd, 19, ref useDark, sizeof(int));
            }
            catch { }
        };
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private static void ShareToTelegram(string filePath)
    {
        Clipboard.SetFileDropList(new StringCollection { filePath });
        var telegram = GetTelegramPath();
        if (telegram is not null)
            Process.Start(new ProcessStartInfo(telegram) { UseShellExecute = true });
        else
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
    }

    private static string? GetTelegramPath()
    {
        string[] paths =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Telegram Desktop", "Telegram.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Telegram Desktop", "Telegram.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Telegram Desktop", "Telegram.exe")
        ];
        return paths.FirstOrDefault(File.Exists);
    }

    private static Button ToolButton(string content, string styleKey) => new()
    {
        Content = content,
        Margin = new Thickness(6, 0, 0, 0),
        Padding = new Thickness(16, 8, 16, 8),
        FontWeight = FontWeights.SemiBold,
        Style = Application.Current?.TryFindResource(styleKey) as Style
    };

    private static string SaveToDocuments(FixedDocument doc, string fileName)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VoltStream");
        Directory.CreateDirectory(dir);
        var safe = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(dir, $"{safe}.pdf");
        SavePdf(doc, path);
        return path;
    }

    public static FixedDocument BuildFixedDocument<T>(ReportDefinition<T> def)
    {
        var doc = new FixedDocument();
        doc.DocumentPaginator.PageSize = new Size(PageWidth, PageHeight);

        var widths = def.Columns.Select(c => c.Width).ToArray();
        var headers = def.Columns.Select(c => c.Header).ToArray();
        double headerHeight = MeasureRow(headers, widths, true);
        var rows = def.Rows.Select(r => RowText(def, r)).ToList();

        var brand = BuildBrandBlock(def, widths.Sum());
        brand.Measure(new Size(widths.Sum(), double.PositiveInfinity));
        double brandHeight = brand.DesiredSize.Height;

        var pageNumbers = new List<TextBlock>();
        int idx = 0;

        do
        {
            bool isFirst = pageNumbers.Count == 0;
            double gridTop = Margin + (isFirst ? brandHeight : 0);
            double maxContent = PageHeight - gridTop - FooterZone;

            var page = new FixedPage { Width = PageWidth, Height = PageHeight, Background = Brushes.White };

            if (isFirst)
            {
                FixedPage.SetLeft(brand, Margin);
                FixedPage.SetTop(brand, Margin);
                page.Children.Add(brand);
            }

            var grid = new Grid();
            foreach (var w in widths)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(w) });

            int rowIndex = 0;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int i = 0; i < headers.Length; i++)
                AddCell(grid, HeaderCell(headers[i], ToAlign(def.Columns[i].Align)), rowIndex, i);
            double used = headerHeight;

            int onThisPage = 0;
            while (idx < rows.Count)
            {
                double rowH = MeasureRow(rows[idx], widths, false);
                double reserve = idx == rows.Count - 1 && def.Totals is not null ? TotalsReserve : 0;
                if (onThisPage > 0 && used + rowH + reserve > maxContent)
                    break;

                rowIndex++;
                var rowBg = idx % 2 == 1 ? Zebra : Brushes.White;
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                for (int i = 0; i < def.Columns.Count; i++)
                    AddCell(grid, BodyCell(rows[idx][i], ToAlign(def.Columns[i].Align), rowBg), rowIndex, i);
                used += rowH;
                onThisPage++;
                idx++;
            }

            if (idx >= rows.Count && def.Totals is not null)
                AddTotalsRow(grid, ref rowIndex, def);

            var pn = new TextBlock { FontSize = 10, Foreground = Muted };
            FixedPage.SetBottom(pn, 18);
            FixedPage.SetRight(pn, Margin);
            page.Children.Add(pn);
            pageNumbers.Add(pn);

            FixedPage.SetLeft(grid, Margin);
            FixedPage.SetTop(grid, gridTop);
            page.Children.Add(grid);

            var pc = new PageContent();
            ((IAddChild)pc).AddChild(page);
            doc.Pages.Add(pc);
        }
        while (idx < rows.Count);

        for (int i = 0; i < pageNumbers.Count; i++)
            pageNumbers[i].Text = $"{i + 1} / {pageNumbers.Count}";

        return doc;
    }

    public static void SavePdf(FixedDocument doc, string path, double dpi = 300)
    {
        using var pdf = new PdfDocument();
        foreach (var pc in doc.Pages)
        {
            var fp = pc.GetPageRoot(false);
            if (fp is null)
                continue;

            fp.Measure(new Size(fp.Width, fp.Height));
            fp.Arrange(new Rect(new Size(fp.Width, fp.Height)));
            fp.UpdateLayout();

            var bmp = new RenderTargetBitmap((int)(fp.Width * dpi / 96), (int)(fp.Height * dpi / 96), dpi, dpi, PixelFormats.Pbgra32);
            bmp.Render(fp);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            ms.Position = 0;

            var pdfPage = pdf.AddPage();
            pdfPage.Width = XUnit.FromPoint(fp.Width);
            pdfPage.Height = XUnit.FromPoint(fp.Height);

            using var gfx = XGraphics.FromPdfPage(pdfPage);
            using var image = XImage.FromStream(ms);
            gfx.DrawImage(image, 0, 0, pdfPage.Width, pdfPage.Height);
        }
        pdf.Save(path);
    }

    private static XLWorkbook BuildWorkbook<T>(ReportDefinition<T> def)
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(def.SheetName);
        int cols = def.Columns.Count;
        int r = 1;

        ws.Cell(r, 1).Value = def.Title;
        ws.Range(r, 1, r, cols).Merge();
        ws.Cell(r, 1).Style.Font.Bold = true;
        ws.Cell(r, 1).Style.Font.FontSize = 14;
        ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(r).Height = 22;
        r++;

        if (!string.IsNullOrWhiteSpace(def.Subtitle))
        {
            ws.Cell(r, 1).Value = def.Subtitle;
            ws.Range(r, 1, r, cols).Merge();
            ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            r++;
        }

        int headerRow = r++;
        for (int i = 0; i < cols; i++)
            ws.Cell(headerRow, i + 1).Value = def.Columns[i].Header;
        var hr = ws.Range(headerRow, 1, headerRow, cols);
        hr.Style.Font.Bold = true;
        hr.Style.Font.FontColor = XLColor.FromArgb(0x4F, 0x46, 0xE5);
        hr.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        hr.Style.Fill.BackgroundColor = XLColor.FromArgb(0xEE, 0xF0, 0xFD);

        foreach (var item in def.Rows)
        {
            for (int i = 0; i < cols; i++)
                SetCell(ws.Cell(r, i + 1), def.Columns[i].Value(item), def.Columns[i].Format, def.Columns[i].IsNumber);
            r++;
        }

        if (def.Totals is not null)
        {
            int span = Math.Max(1, def.Totals.LabelSpan);
            ws.Cell(r, 1).Value = def.Totals.Label;
            if (span > 1)
                ws.Range(r, 1, r, span).Merge();
            ws.Cell(r, 1).Style.Font.Bold = true;
            foreach (var tc in def.Totals.Cells)
            {
                var col = def.Columns[tc.Column];
                var cell = ws.Cell(r, tc.Column + 1);
                SetCell(cell, tc.Value, tc.Format ?? col.Format, col.IsNumber);
                cell.Style.Font.Bold = true;
            }
        }

        ws.Columns().AdjustToContents();
        return wb;
    }

    private static void SetCell(IXLCell cell, object? value, string? format, bool isNumber)
    {
        switch (value)
        {
            case null: cell.Value = string.Empty; break;
            case decimal d: cell.Value = d; break;
            case double db: cell.Value = db; break;
            case int n: cell.Value = n; break;
            case long l: cell.Value = l; break;
            case DateTime dt: cell.Value = dt; cell.Style.DateFormat.Format = "dd.MM.yyyy"; break;
            default: cell.Value = value.ToString(); break;
        }
        if (isNumber && format is not null)
            cell.Style.NumberFormat.Format = ExcelFormat(format);
    }

    private static string ExcelFormat(string format) => format switch
    {
        "N0" => "#,##0",
        "N2" => "#,##0.00",
        _ => format
    };

    private static FrameworkElement BuildBrandBlock<T>(ReportDefinition<T> def, double width)
    {
        var panel = new StackPanel { Width = width };

        var head = new Grid();
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = new StackPanel();
        left.Children.Add(Line(Brand, 12, true, Accent, TextAlignment.Left, new Thickness(0, 0, 0, 1)));
        left.Children.Add(Line(def.Title, 19, true, Ink, TextAlignment.Left));
        if (!string.IsNullOrWhiteSpace(def.Subtitle))
            left.Children.Add(Line(def.Subtitle!, 11, false, Muted, TextAlignment.Left, new Thickness(0, 3, 0, 0)));
        Grid.SetColumn(left, 0);
        head.Children.Add(left);

        var date = Line($"{DateTime.Now:dd.MM.yyyy  HH:mm}", 10, false, Muted, TextAlignment.Right);
        date.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetColumn(date, 1);
        head.Children.Add(date);

        panel.Children.Add(head);
        panel.Children.Add(new Border { Height = 2.5, Background = Accent, Margin = new Thickness(0, 8, 0, 12) });
        return panel;

        static TextBlock Line(string text, double size, bool bold, Brush brush, TextAlignment align, Thickness margin = default) => new()
        {
            Text = text,
            FontSize = size,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = brush,
            TextAlignment = align,
            Margin = margin
        };
    }

    private static void AddTotalsRow<T>(Grid grid, ref int rowIndex, ReportDefinition<T> def)
    {
        var totals = def.Totals!;
        rowIndex++;
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        if (totals.Cells.Count == 1)
        {
            var tc = totals.Cells[0];
            var col = def.Columns[tc.Column];
            string valueText = FormatValue(tc.Value, tc.Format ?? col.Format);

            var inner = new Grid();
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lbl = TotalTextBlock(totals.Label, TextAlignment.Left);
            var val = TotalTextBlock(valueText, TextAlignment.Right);
            Grid.SetColumn(val, 1);
            inner.Children.Add(lbl);
            inner.Children.Add(val);

            var border = new Border
            {
                Background = HeaderBg,
                BorderBrush = Accent,
                BorderThickness = new Thickness(0, 1.5, 0, 0),
                Padding = new Thickness(8, 6, 8, 6),
                Child = inner
            };
            Grid.SetRow(border, rowIndex);
            Grid.SetColumn(border, 0);
            Grid.SetColumnSpan(border, def.Columns.Count);
            grid.Children.Add(border);
            return;
        }

        int span = Math.Max(1, totals.LabelSpan);
        var label = TotalCell(totals.Label, TextAlignment.Left, new Thickness(6, 6, 10, 6));
        Grid.SetRow(label, rowIndex);
        Grid.SetColumn(label, 0);
        Grid.SetColumnSpan(label, span);
        grid.Children.Add(label);

        var byColumn = totals.Cells.ToDictionary(c => c.Column);
        for (int i = span; i < def.Columns.Count; i++)
        {
            string text = byColumn.TryGetValue(i, out var tc2) ? FormatValue(tc2.Value, tc2.Format ?? def.Columns[i].Format) : string.Empty;
            AddCell(grid, TotalCell(text, ToAlign(def.Columns[i].Align), new Thickness(6)), rowIndex, i);
        }
    }

    private static TextBlock TotalTextBlock(string text, TextAlignment align) => new()
    {
        Text = text,
        FontSize = 11.5,
        FontWeight = FontWeights.Bold,
        Foreground = Accent,
        TextAlignment = align,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static string[] RowText<T>(ReportDefinition<T> def, T item)
        => def.Columns.Select(c => FormatValue(c.Value(item), c.Format)).ToArray();

    private static string FormatValue(object? value, string? format)
    {
        if (value is null)
            return string.Empty;
        if (format is not null && value is IFormattable f)
            return f.ToString(format, CultureInfo.CurrentCulture);
        return value.ToString() ?? string.Empty;
    }

    private static double MeasureRow(string[] values, double[] widths, bool isHeader)
    {
        var grid = new Grid();
        foreach (var w in widths)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(w) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int i = 0; i < values.Length; i++)
            AddCell(grid, isHeader ? HeaderCell(values[i], TextAlignment.Center) : BodyCell(values[i], TextAlignment.Left, Brushes.White), 0, i);
        grid.Measure(new Size(widths.Sum(), double.PositiveInfinity));
        return grid.DesiredSize.Height;
    }

    private static Border HeaderCell(string text, TextAlignment align)
        => BuildCell(text, 11.5, true, align, new Thickness(6, 7, 6, 7), HeaderBg, Accent, new Thickness(0, 0, 0, 1.5), Accent);

    private static Border BodyCell(string text, TextAlignment align, Brush background)
        => BuildCell(text, 11, false, align, new Thickness(6, 6, 6, 6), background, Ink, new Thickness(0, 0, 0, 0.6), RowLine);

    private static Border TotalCell(string text, TextAlignment align, Thickness padding)
        => BuildCell(text, 11.5, true, align, padding, HeaderBg, Accent, new Thickness(0, 1.5, 0, 0), Accent);

    private static Border BuildCell(string text, double fontSize, bool bold, TextAlignment align, Thickness padding, Brush background, Brush foreground, Thickness borderThickness, Brush borderBrush) => new()
    {
        Background = background,
        BorderBrush = borderBrush,
        BorderThickness = borderThickness,
        Padding = padding,
        Child = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = foreground,
            TextAlignment = align,
            TextWrapping = TextWrapping.Wrap
        }
    };

    private static void AddCell(Grid grid, UIElement cell, int row, int column)
    {
        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, column);
        grid.Children.Add(cell);
    }

    private static TextAlignment ToAlign(ReportAlign align) => align switch
    {
        ReportAlign.Right => TextAlignment.Right,
        ReportAlign.Center => TextAlignment.Center,
        _ => TextAlignment.Left
    };

    private static Brush Freeze(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}
