namespace VoltStream.Mobile;

public class App : Application
{
    public App() => UserAppTheme = AppTheme.Light;

    protected override Window CreateWindow(IActivationState? activationState)
        => new(new AppShell()) { Title = "Volt Stream" };
}
