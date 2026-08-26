namespace CompositionMaterial.Avalonia;

/// <summary>Describes the rendering backend currently used by a material control.</summary>
public enum MaterialRenderingMode
{
    /// <summary>The configured Avalonia fallback brush is being rendered.</summary>
    Fallback,

    /// <summary>A native Windows.UI.Composition visual is active.</summary>
    WinUIComposition,
}
