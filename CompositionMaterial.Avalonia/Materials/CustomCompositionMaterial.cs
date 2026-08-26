using Avalonia;

namespace CompositionMaterial.Avalonia.Materials;

/// <summary>A material described by a user-defined brush graph.</summary>
public sealed class CustomCompositionMaterial : CompositionMaterial
{
    public static readonly StyledProperty<MaterialBrushNode?> RootBrushProperty =
        AvaloniaProperty.Register<CustomCompositionMaterial, MaterialBrushNode?>(nameof(RootBrush));

    [global::Avalonia.Metadata.Content]
    public MaterialBrushNode? RootBrush
    {
        get => GetValue(RootBrushProperty);
        set => SetValue(RootBrushProperty, value);
    }
}
