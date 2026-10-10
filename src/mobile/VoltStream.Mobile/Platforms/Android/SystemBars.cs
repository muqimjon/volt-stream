namespace VoltStream.Mobile;

using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Webkit;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;
using Java.Interop;

public sealed class SystemBars : Java.Lang.Object, IOnApplyWindowInsetsListener
{
    private readonly Activity activity;
    private Color top;
    private Color bottom;
    private int left, topInset, right, bottomInset;

    public static SystemBars? Current { get; private set; }

    public SystemBars(Activity activity)
    {
        this.activity = activity;
        var night = (activity.Resources!.Configuration!.UiMode & UiMode.NightMask) == UiMode.NightYes;
        top = Color.ParseColor(night ? "#141A2A" : "#4F46E5");
        bottom = Color.ParseColor(night ? "#141A2A" : "#FFFFFF");

        var window = activity.Window!;
        WindowCompat.SetDecorFitsSystemWindows(window, false);
#pragma warning disable CA1422
        window.SetStatusBarColor(Color.Transparent);
        window.SetNavigationBarColor(Color.Transparent);
#pragma warning restore CA1422
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
            window.NavigationBarContrastEnforced = false;

        ViewCompat.SetOnApplyWindowInsetsListener(window.DecorView, this);
        Current = this;
        Redraw();
    }

    public WindowInsetsCompat? OnApplyWindowInsets(View? v, WindowInsetsCompat? windowInsets)
    {
        var bars = windowInsets!.GetInsets(WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout())!;
        var ime = windowInsets.GetInsets(WindowInsetsCompat.Type.Ime())!;
        (left, topInset, right, bottomInset) = (bars.Left, bars.Top, bars.Right, Math.Max(bars.Bottom, ime.Bottom));
        Redraw();
        return WindowInsetsCompat.Consumed;
    }

    public void Update(string? statusColor, string? navigationColor)
    {
        if (!TryParse(statusColor, out var newTop) || !TryParse(navigationColor, out var newBottom)) return;
        activity.RunOnUiThread(() => { top = newTop; bottom = newBottom; Redraw(); });
    }

    private void Redraw()
    {
        var window = activity.Window!;
        activity.FindViewById(Android.Resource.Id.Content)!.SetPadding(left, topInset, right, bottomInset);

        var layers = new LayerDrawable([new ColorDrawable(bottom), new ColorDrawable(top)]);
        layers.SetLayerGravity(1, GravityFlags.Top);
        layers.SetLayerHeight(1, topInset);
        window.SetBackgroundDrawable(layers);

        var controller = WindowCompat.GetInsetsController(window, window.DecorView)!;
        controller.AppearanceLightStatusBars = ColorUtils.CalculateLuminance(top.ToArgb()) > 0.5;
        controller.AppearanceLightNavigationBars = ColorUtils.CalculateLuminance(bottom.ToArgb()) > 0.5;
    }

    private static bool TryParse(string? value, out Color color)
    {
        color = default;
        try
        {
            color = Color.ParseColor(value!.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class ThemeBridge : Java.Lang.Object
{
    [JavascriptInterface, Export("bars")]
    public void Bars(string? statusColor, string? navigationColor)
        => SystemBars.Current?.Update(statusColor, navigationColor);
}
