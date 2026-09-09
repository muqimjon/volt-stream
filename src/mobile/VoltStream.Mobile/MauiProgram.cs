namespace VoltStream.Mobile;

using Microsoft.Maui.Handlers;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        WebViewHandler.Mapper.AppendToMapping(nameof(VoltStream), static (handler, _) =>
        {
            var settings = handler.PlatformView.Settings;
            settings.JavaScriptEnabled = true;
            settings.DomStorageEnabled = true;
            settings.DatabaseEnabled = true;
        });

        return MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            .Build();
    }
}
