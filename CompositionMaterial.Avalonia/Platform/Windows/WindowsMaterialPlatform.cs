using CompositionMaterial.Avalonia.Internal;

namespace CompositionMaterial.Avalonia.Platform.Windows;

internal static class WindowsMaterialPlatform
{
    public static IMaterialControlAttachment? TryAttach(CompositionMaterialControl control)
    {
        if (!OperatingSystem.IsWindows())
        {
            control.SetPlatformMode(MaterialRenderingMode.Fallback);
            return null;
        }

        return TopLevelMaterialHost.TryAttach(control);
    }
}
