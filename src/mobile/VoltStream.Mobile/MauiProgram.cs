namespace VoltStream.Mobile;

using Microsoft.Maui.Handlers;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        WebViewHandler.Mapper.AppendToMapping(nameof(VoltStream), static (handler, _) =>
        {
            var web = handler.PlatformView;
            web.Settings.JavaScriptEnabled = true;
            web.Settings.DomStorageEnabled = true;
            web.Settings.DatabaseEnabled = true;
            web.AddJavascriptInterface(new ThemeBridge(), "VoltShell");
        });

        return MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            .Build();
    }
}
