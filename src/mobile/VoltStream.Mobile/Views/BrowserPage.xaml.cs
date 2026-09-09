namespace VoltStream.Mobile.Views;

public partial class BrowserPage : ContentPage
{
    private string? loadedUrl;
    private int loadedRevision = -1;
    private bool firstLoadPending;

    public BrowserPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (loadedUrl is null || loadedRevision != Settings.Revision)
            await ConnectAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        if (!error.IsVisible && web.CanGoBack)
        {
            web.GoBack();
            return true;
        }

        return false;
    }

    private async Task ConnectAsync()
    {
        busy.IsVisible = true;
        error.IsVisible = false;

        var url = await ServerHealth.ResolveAsync();
        if (url is null)
        {
            busy.IsVisible = false;
            error.IsVisible = true;
            return;
        }

        loadedUrl = url;
        loadedRevision = Settings.Revision;
        firstLoadPending = true;
        web.Source = new UrlWebViewSource { Url = url };
    }

    private void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (firstLoadPending && e.Result is not WebNavigationResult.Success)
        {
            loadedUrl = null;
            busy.IsVisible = false;
            error.IsVisible = true;
            return;
        }

        firstLoadPending = false;
        busy.IsVisible = false;
    }

    private async void OnRetry(object? sender, EventArgs e) => await ConnectAsync();

    private async void OnSettings(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//connection");
}
