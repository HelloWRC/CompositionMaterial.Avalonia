using Avalonia;
using Avalonia.Collections;

namespace CompositionMaterial.Avalonia.Materials;

public sealed class GaussianBlurBrushNode : UnaryMaterialBrushNode
{
    public static readonly StyledProperty<double> AmountProperty =
        AvaloniaProperty.Register<GaussianBlurBrushNode, double>(nameof(Amount), 30, coerce: (_, value) => Math.Clamp(value, 0, 250));
    public double Amount { get => GetValue(AmountProperty); set => SetValue(AmountProperty, value); }
}

public sealed class SaturationBrushNode : UnaryMaterialBrushNode
{
    public static readonly StyledProperty<double> AmountProperty =
        AvaloniaProperty.Register<SaturationBrushNode, double>(nameof(Amount), 1, coerce: (_, value) => Math.Clamp(value, 0, 8));
    public double Amount { get => GetValue(AmountProperty); set => SetValue(AmountProperty, value); }
}

public sealed class OpacityBrushNode : UnaryMaterialBrushNode
{
    public static readonly StyledProperty<double> OpacityProperty =
        AvaloniaProperty.Register<OpacityBrushNode, double>(nameof(Opacity), 1, coerce: (_, value) => Math.Clamp(value, 0, 1));
    public double Opacity { get => GetValue(OpacityProperty); set => SetValue(OpacityProperty, value); }
}

public sealed class TintBrushNode : UnaryMaterialBrushNode
{
    public static readonly StyledProperty<global::Avalonia.Media.Color> ColorProperty =
        AvaloniaProperty.Register<TintBrushNode, global::Avalonia.Media.Color>(nameof(Color), global::Avalonia.Media.Colors.Transparent);
    public static readonly StyledProperty<double> OpacityProperty =
        AvaloniaProperty.Register<TintBrushNode, double>(nameof(Opacity), 0.5, coerce: (_, value) => Math.Clamp(value, 0, 1));

    public global::Avalonia.Media.Color Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public double Opacity { get => GetValue(OpacityProperty); set => SetValue(OpacityProperty, value); }
}

public sealed class BlendBrushNode : MaterialBrushNode
{
    public static readonly StyledProperty<MaterialBrushNode?> BackgroundProperty =
        AvaloniaProperty.Register<BlendBrushNode, MaterialBrushNode?>(nameof(Background));
    public static readonly StyledProperty<MaterialBrushNode?> ForegroundProperty =
        AvaloniaProperty.Register<BlendBrushNode, MaterialBrushNode?>(nameof(Foreground));
    public static readonly StyledProperty<MaterialBlendMode> ModeProperty =
        AvaloniaProperty.Register<BlendBrushNode, MaterialBlendMode>(nameof(Mode), MaterialBlendMode.SoftLight);

    public MaterialBrushNode? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    [global::Avalonia.Metadata.Content]
    public MaterialBrushNode? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
    public MaterialBlendMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
}

public sealed class CompositeBrushNode : MaterialBrushNode
{
    [global::Avalonia.Metadata.Content]
    public AvaloniaList<MaterialBrushNode> Sources { get; } = [];
}

public sealed class MaskBrushNode : MaterialBrushNode
{
    public static readonly StyledProperty<MaterialBrushNode?> SourceProperty =
        AvaloniaProperty.Register<MaskBrushNode, MaterialBrushNode?>(nameof(Source));
    public static readonly StyledProperty<MaterialBrushNode?> MaskProperty =
        AvaloniaProperty.Register<MaskBrushNode, MaterialBrushNode?>(nameof(Mask));

    public MaterialBrushNode? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    [global::Avalonia.Metadata.Content]
    public MaterialBrushNode? Mask { get => GetValue(MaskProperty); set => SetValue(MaskProperty, value); }
}
