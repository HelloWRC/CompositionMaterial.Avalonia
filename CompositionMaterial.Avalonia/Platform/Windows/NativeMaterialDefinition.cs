namespace CompositionMaterial.Avalonia.Platform.Windows;

internal sealed record NativeMaterialDefinition(object PrimaryBrush, object? FrostBrush = null, float FrostOpacity = 0)
{
    public void Dispose()
    {
        NativeWindowContext.DisposeProxy(FrostBrush);
        NativeWindowContext.DisposeProxy(PrimaryBrush);
    }
}
