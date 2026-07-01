namespace ApiServices.Models;

public sealed class PagingScope : IDisposable
{
    private static readonly AsyncLocal<Box?> current = new();

    private sealed class Box { public PagedListMetadata? Value; }

    private PagingScope() { }

    public static PagingScope Begin()
    {
        current.Value = new Box();
        return new PagingScope();
    }

    public static PagedListMetadata? Result => current.Value?.Value;

    internal static void Capture(PagedListMetadata metadata)
    {
        if (current.Value is { } box) box.Value = metadata;
    }

    public void Dispose() => current.Value = null;
}
