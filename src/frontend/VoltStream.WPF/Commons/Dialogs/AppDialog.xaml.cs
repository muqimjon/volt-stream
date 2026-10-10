namespace VoltStream.WPF.Commons.Dialogs;

using FontAwesome.Sharp;
using System.Windows;
using System.Windows.Input;
using VoltStream.WPF.Commons.Localization;

public partial class AppDialog : Window
{
    private AppDialog(string message, string title, IconChar icon, string accent, bool confirm)
    {
        InitializeComponent();

        TitleText.Text = Title = title;
        MessageText.Text = message;
        IconImage.Icon = icon;
        IconImage.SetResourceReference(ForegroundProperty, accent);

        AcceptButton.Content = TranslationSource.T(confirm ? "Common.Yes" : "Common.Ok");
        CancelButton.Content = TranslationSource.T("Common.No");
        CancelButton.Visibility = confirm ? Visibility.Visible : Visibility.Collapsed;
    }

    public static bool Confirm(string message, string title, Window? owner = null)
        => Show(message, title, IconChar.CircleQuestion, "Brand", true, owner);

    public static void Info(string message, string title, Window? owner = null)
        => Show(message, title, IconChar.CircleInfo, "Info", false, owner);

    public static void Warning(string message, string title, Window? owner = null)
        => Show(message, title, IconChar.TriangleExclamation, "Warning", false, owner);

    public static void Error(string message, string title, Window? owner = null)
        => Show(message, title, IconChar.CircleExclamation, "Danger", false, owner);

    private static bool Show(string message, string title, IconChar icon, string accent, bool confirm, Window? owner)
    {
        var dialog = new AppDialog(message, title, icon, accent, confirm);

        if (Resolve(owner) is { } host) dialog.Owner = host;
        else dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        return dialog.ShowDialog() == true;
    }

    private static Window? Resolve(Window? owner)
    {
        if (owner is { IsLoaded: true }) return owner;

        var active = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsLoaded && w.IsActive);
        return active ?? (Application.Current.MainWindow is { IsLoaded: true } main ? main : null);
    }

    private void Accept_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            e.Handled = true;
        }
        else base.OnKeyDown(e);
    }
}
