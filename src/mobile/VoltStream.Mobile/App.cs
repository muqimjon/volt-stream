namespace VoltStream.Mobile;

public class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
        => new(new AppShell()) { Title = "Volt Stream" };
}
