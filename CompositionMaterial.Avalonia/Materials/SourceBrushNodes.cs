using Avalonia;
using Avalonia.Collections;
using Avalonia.Media;

namespace CompositionMaterial.Avalonia.Materials;

public sealed class BackdropBrushNode : MaterialBrushNode
{
    public static readonly StyledProperty<BackdropKind> KindProperty =
        AvaloniaProperty.Register<BackdropBrushNode, BackdropKind>(nameof(Kind));

    public BackdropKind Kind { get => GetValue(KindProperty); set => SetValue(KindProperty, value); }
}

public sealed class ColorBrushNode : MaterialBrushNode
{
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<ColorBrushNode, Color>(nameof(Color), Colors.Transparent);

    public Color Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
}

public sealed class MaterialGradientStop : AvaloniaObject
{
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<MaterialGradientStop, Color>(nameof(Color), Colors.Transparent);
    public static readonly StyledProperty<double> OffsetProperty =
        AvaloniaProperty.Register<MaterialGradientStop, double>(nameof(Offset));

    public Color Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public double Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }
}

public abstract class GradientBrushNode : MaterialBrushNode
{
    [global::Avalonia.Metadata.Content]
    public AvaloniaList<MaterialGradientStop> GradientStops { get; } = [];
}

public sealed class LinearGradientBrushNode : GradientBrushNode
{
    public static readonly StyledProperty<RelativePoint> StartPointProperty =
        AvaloniaProperty.Register<LinearGradientBrushNode, RelativePoint>(nameof(StartPoint), RelativePoint.TopLeft);
    public static readonly StyledProperty<RelativePoint> EndPointProperty =
        AvaloniaProperty.Register<LinearGradientBrushNode, RelativePoint>(nameof(EndPoint), RelativePoint.BottomRight);

    public RelativePoint StartPoint { get => GetValue(StartPointProperty); set => SetValue(StartPointProperty, value); }
    public RelativePoint EndPoint { get => GetValue(EndPointProperty); set => SetValue(EndPointProperty, value); }
}

public sealed class RadialGradientBrushNode : GradientBrushNode
{
    public static readonly StyledProperty<RelativePoint> CenterProperty =
        AvaloniaProperty.Register<RadialGradientBrushNode, RelativePoint>(nameof(Center), RelativePoint.Center);
    public static readonly StyledProperty<double> RadiusProperty =
        AvaloniaProperty.Register<RadialGradientBrushNode, double>(nameof(Radius), 0.5);

    public RelativePoint Center { get => GetValue(CenterProperty); set => SetValue(CenterProperty, value); }
    public double Radius { get => GetValue(RadiusProperty); set => SetValue(RadiusProperty, value); }
}

public sealed class NoiseBrushNode : MaterialBrushNode
{
    public static readonly StyledProperty<double> OpacityProperty =
        AvaloniaProperty.Register<NoiseBrushNode, double>(nameof(Opacity), 0.025, coerce: (_, value) => Math.Clamp(value, 0, 1));
    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<NoiseBrushNode, double>(nameof(Scale), 1.0, coerce: (_, value) => Math.Max(0.01, value));

    public double Opacity { get => GetValue(OpacityProperty); set => SetValue(OpacityProperty, value); }
    public double Scale { get => GetValue(ScaleProperty); set => SetValue(ScaleProperty, value); }
}
