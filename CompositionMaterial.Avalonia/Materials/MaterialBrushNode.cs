using Avalonia;

namespace CompositionMaterial.Avalonia.Materials;

/// <summary>Base class for nodes in a declarative material brush graph.</summary>
public abstract class MaterialBrushNode : AvaloniaObject;

/// <summary>Base class for nodes with one input source.</summary>
public abstract class UnaryMaterialBrushNode : MaterialBrushNode
{
    public static readonly StyledProperty<MaterialBrushNode?> SourceProperty =
        AvaloniaProperty.Register<UnaryMaterialBrushNode, MaterialBrushNode?>(nameof(Source));

    [global::Avalonia.Metadata.Content]
    public MaterialBrushNode? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }
}
