namespace VoltStream.WPF.Commons.ViewModels;

using FontAwesome.Sharp;
using VoltStream.WPF.Commons.Localization;

public sealed class PaletteItem(string titleKey, IconChar icon, Action invoke)
{
    public string TitleKey { get; } = titleKey;
    public IconChar Icon { get; } = icon;
    public Action Invoke { get; } = invoke;
    public string Title => TranslationSource.T(TitleKey);
}
