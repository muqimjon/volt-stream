namespace VoltStream.WPF.Home.Controls;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

public partial class LineChart : UserControl
{
    private const double Left = 64, Right = 18, Top = 36, Bottom = 30;

    private readonly List<UIElement> hoverLayer = [];
    private double[] xs = [];
    private List<(ChartSeries series, double[] ys, Color color)> plotted = [];
    private IReadOnlyList<string> labels = [];

    public LineChart()
    {
        InitializeComponent();
        Loaded += (_, _) => Render();
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(ChartData), typeof(LineChart),
            new PropertyMetadata(null, (d, _) => ((LineChart)d).Render()));

    public ChartData? Data { get => (ChartData?)GetValue(DataProperty); set => SetValue(DataProperty, value); }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => Render();

    private void Render()
    {
        canvas.Children.Clear();
        hoverLayer.Clear();
        xs = [];
        plotted = [];

        double w = canvas.ActualWidth, h = canvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        labels = Data?.Labels ?? [];
        var series = Data?.Series?.Where(s => s.Values.Count > 0).ToList() ?? [];
        if (labels.Count == 0 || series.Count == 0) return;

        int count = labels.Count;
        double plotW = w - Left - Right, plotH = h - Top - Bottom;
        if (plotW <= 0 || plotH <= 0) return;

        double maxValue = NiceCeil(series.Max(s => s.Values.Count > 0 ? s.Values.Max() : 0));
        if (maxValue <= 0) maxValue = 1;

        var gridBrush = Res("Border", Colors.Gainsboro);
        var labelBrush = Res("TextTertiary", Colors.Gray);

        const int ticks = 4;
        for (int t = 0; t <= ticks; t++)
        {
            double val = maxValue * t / ticks;
            double y = Top + plotH * (1 - (double)t / ticks);
            canvas.Children.Add(new Line { X1 = Left, Y1 = y, X2 = Left + plotW, Y2 = y, Stroke = gridBrush, StrokeThickness = 1, Opacity = 0.5 });

            var lbl = new TextBlock { Text = Compact(val), FontSize = 11, Foreground = labelBrush };
            lbl.Measure(new Size(Left, 20));
            Canvas.SetLeft(lbl, Left - 8 - lbl.DesiredSize.Width);
            Canvas.SetTop(lbl, y - 8);
            canvas.Children.Add(lbl);
        }

        xs = new double[count];
        for (int i = 0; i < count; i++)
            xs[i] = count == 1 ? Left + plotW / 2 : Left + plotW * i / (count - 1);
        double Y(double v) => Top + plotH * (1 - v / maxValue);
        double baseY = Top + plotH;

        foreach (var s in series)
        {
            var sc = Adjust(ResColor(s.ColorKey));
            var pts = new List<Point>();
            for (int i = 0; i < count; i++)
                pts.Add(new Point(xs[i], Y(i < s.Values.Count ? s.Values[i] : 0)));

            canvas.Children.Add(new Path
            {
                Data = new PathGeometry([BuildSmoothFigure(pts, true, baseY)]),
                Fill = new LinearGradientBrush(
                    Color.FromArgb(0x38, sc.R, sc.G, sc.B),
                    Color.FromArgb(0x00, sc.R, sc.G, sc.B), 90)
            });

            canvas.Children.Add(new Path
            {
                Data = new PathGeometry([BuildSmoothFigure(pts, false, baseY)]),
                Stroke = new SolidColorBrush(sc),
                StrokeThickness = 2.6,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            });

            plotted.Add((s, [.. pts.Select(p => p.Y)], sc));
        }

        int maxLabels = Math.Max(2, (int)(plotW / 70));
        int step = (int)Math.Ceiling((double)count / maxLabels);
        for (int i = 0; i < count; i += step)
        {
            var lbl = new TextBlock { Text = labels[i], FontSize = 11, Foreground = labelBrush };
            lbl.Measure(new Size(80, 20));
            Canvas.SetLeft(lbl, xs[i] - lbl.DesiredSize.Width / 2);
            Canvas.SetTop(lbl, baseY + 8);
            canvas.Children.Add(lbl);
        }

        double lx = Left;
        var legendBrush = Res("TextSecondary", Colors.Gray);
        foreach (var s in series)
        {
            var swatch = new Rectangle
            {
                Width = 14,
                Height = 4,
                RadiusX = 2,
                RadiusY = 2,
                Fill = new SolidColorBrush(Adjust(ResColor(s.ColorKey)))
            };
            Canvas.SetLeft(swatch, lx);
            Canvas.SetTop(swatch, 12);
            canvas.Children.Add(swatch);

            var lt = new TextBlock { Text = s.Name, FontSize = 11.5, Foreground = legendBrush };
            lt.Measure(new Size(160, 20));
            Canvas.SetLeft(lt, lx + 18);
            Canvas.SetTop(lt, 6);
            canvas.Children.Add(lt);
            lx += 18 + lt.DesiredSize.Width + 16;
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (xs.Length == 0) return;

        var mouse = e.GetPosition(canvas);
        int nearest = 0;
        double best = double.MaxValue;
        for (int i = 0; i < xs.Length; i++)
        {
            double d = Math.Abs(xs[i] - mouse.X);
            if (d < best) { best = d; nearest = i; }
        }

        ClearHover();

        double gx = xs[nearest];
        AddHover(new Line
        {
            X1 = gx,
            Y1 = Top,
            X2 = gx,
            Y2 = canvas.ActualHeight - Bottom,
            Stroke = Res("TextTertiary", Colors.Gray),
            StrokeThickness = 1,
            StrokeDashArray = [3, 3]
        });

        var dotFill = Res("Surface", Colors.White);
        foreach (var (_, ys, col) in plotted)
        {
            var dot = new Ellipse
            {
                Width = 9,
                Height = 9,
                Fill = dotFill,
                Stroke = new SolidColorBrush(col),
                StrokeThickness = 2
            };
            Canvas.SetLeft(dot, gx - 4.5);
            Canvas.SetTop(dot, ys[nearest] - 4.5);
            AddHover(dot);
        }

        var box = new StackPanel();
        box.Children.Add(new TextBlock
        {
            Text = labels[nearest],
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = Res("TextSecondary", Colors.Gray),
            Margin = new Thickness(0, 0, 0, 3)
        });
        foreach (var (s, _, col) in plotted)
        {
            double v = s.Values[nearest];
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };
            row.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(col), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            row.Children.Add(new TextBlock { Text = $"{s.Name}: {v:N0}", FontSize = 12, Foreground = Res("TextPrimary", Colors.Black) });
            box.Children.Add(row);
        }
        var tip = new Border
        {
            Background = Res("Surface", Colors.White),
            BorderBrush = Res("Border", Colors.Gainsboro),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 7, 10, 7),
            Child = box,
            Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 1, Opacity = 0.18, Color = Colors.Black }
        };
        tip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double tw = tip.DesiredSize.Width, thh = tip.DesiredSize.Height;
        double tx = gx + 14;
        if (tx + tw > canvas.ActualWidth) tx = gx - 14 - tw;
        double ty = Math.Max(Top, Math.Min(mouse.Y - thh / 2, canvas.ActualHeight - Bottom - thh));
        Canvas.SetLeft(tip, tx);
        Canvas.SetTop(tip, ty);
        AddHover(tip);
    }

    private void OnMouseLeave(object sender, MouseEventArgs e) => ClearHover();

    private void AddHover(UIElement el)
    {
        el.IsHitTestVisible = false;
        hoverLayer.Add(el);
        canvas.Children.Add(el);
    }

    private void ClearHover()
    {
        foreach (var el in hoverLayer) canvas.Children.Remove(el);
        hoverLayer.Clear();
    }

    private static PathFigure BuildSmoothFigure(IReadOnlyList<Point> pts, bool closeToBaseline, double baseY)
    {
        var fig = new PathFigure { StartPoint = pts[0], IsFilled = closeToBaseline };
        for (int i = 0; i < pts.Count - 1; i++)
        {
            var p0 = pts[Math.Max(0, i - 1)];
            var p1 = pts[i];
            var p2 = pts[i + 1];
            var p3 = pts[Math.Min(pts.Count - 1, i + 2)];
            var c1 = new Point(p1.X + (p2.X - p0.X) / 6.0, p1.Y + (p2.Y - p0.Y) / 6.0);
            var c2 = new Point(p2.X - (p3.X - p1.X) / 6.0, p2.Y - (p3.Y - p1.Y) / 6.0);
            fig.Segments.Add(new BezierSegment(c1, c2, p2, true));
        }
        if (closeToBaseline)
        {
            fig.Segments.Add(new LineSegment(new Point(pts[^1].X, baseY), true));
            fig.Segments.Add(new LineSegment(new Point(pts[0].X, baseY), true));
            fig.IsClosed = true;
        }
        return fig;
    }

    private static Brush Res(string key, Color fallback)
        => Application.Current?.TryFindResource(key) as Brush ?? new SolidColorBrush(fallback);

    private static Color ResColor(string key)
        => Application.Current?.TryFindResource(key) is Color c ? c : Colors.Gray;

    private static bool IsDark()
        => Application.Current?.TryFindResource("TextPrimary") is SolidColorBrush b
           && (b.Color.R * 0.299 + b.Color.G * 0.587 + b.Color.B * 0.114) > 140;

    private static Color Adjust(Color c)
    {
        if (!IsDark()) return c;
        double lum = c.R * 0.299 + c.G * 0.587 + c.B * 0.114;
        if (lum >= 120) return c;
        const double f = 0.45;
        return Color.FromRgb(
            (byte)(c.R + (255 - c.R) * f),
            (byte)(c.G + (255 - c.G) * f),
            (byte)(c.B + (255 - c.B) * f));
    }

    private static double NiceCeil(double v)
    {
        if (v <= 0) return 1;
        double mag = Math.Pow(10, Math.Floor(Math.Log10(v)));
        double n = v / mag;
        double nice = n <= 1 ? 1 : n <= 2 ? 2 : n <= 2.5 ? 2.5 : n <= 5 ? 5 : 10;
        return nice * mag;
    }

    private static string Compact(double v)
    {
        if (Math.Abs(v) >= 1_000_000_000) return $"{v / 1_000_000_000:0.#}B";
        if (Math.Abs(v) >= 1_000_000) return $"{v / 1_000_000:0.#}M";
        if (Math.Abs(v) >= 1_000) return $"{v / 1_000:0.#}K";
        return v.ToString("0");
    }
}

public record ChartSeries
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<double> Values { get; init; } = [];
    public string ColorKey { get; init; } = "Brand";
}

public record ChartData
{
    public IReadOnlyList<string> Labels { get; init; } = [];
    public IReadOnlyList<ChartSeries> Series { get; init; } = [];
}
