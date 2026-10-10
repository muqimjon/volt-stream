namespace VoltStream.Mobile.Views;

public partial class ConnectionPage : ContentPage
{
    public ConnectionPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        primary.Text = Settings.PrimaryUrl;
        backup.Text = Settings.BackupUrl;
    }

    private async void OnCheck(object? sender, EventArgs e)
    {
        status.Text = Texts.Checking;
        status.Text = $"{Texts.PrimaryUrl}: {await DescribeAsync(primary.Text)}\n" +
                      $"{Texts.BackupUrl}: {await DescribeAsync(backup.Text)}";
    }

    private async void OnSave(object? sender, EventArgs e)
    {
        var primaryUrl = ServerHealth.Normalize(primary.Text);
        var backupUrl = ServerHealth.Normalize(backup.Text);

        if (primaryUrl is null && backupUrl is null)
        {
            status.Text = Texts.AddressRequired;
            return;
        }

        Settings.PrimaryUrl = primaryUrl ?? string.Empty;
        Settings.BackupUrl = backupUrl ?? string.Empty;
        await Shell.Current.GoToAsync("//browser");
    }

    private static async Task<string> DescribeAsync(string? url)
        => string.IsNullOrWhiteSpace(url) ? Texts.Empty
            : await ServerHealth.IsAliveAsync(url) ? Texts.Alive : Texts.Dead;
}
