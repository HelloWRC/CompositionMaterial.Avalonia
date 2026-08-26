using Avalonia;
using Avalonia.Media;

namespace CompositionMaterial.Avalonia.Materials;

public sealed class AcrylicMaterial : CompositionMaterial
{
    public static readonly StyledProperty<Color> TintColorProperty =
        AvaloniaProperty.Register<AcrylicMaterial, Color>(nameof(TintColor), Color.FromRgb(32, 32, 32));
    public static readonly StyledProperty<double> TintOpacityProperty =
        AvaloniaProperty.Register<AcrylicMaterial, double>(nameof(TintOpacity), 0.72, coerce: (_, value) => Math.Clamp(value, 0, 1));
    public static readonly StyledProperty<double> TintLuminosityOpacityProperty =
        AvaloniaProperty.Register<AcrylicMaterial, double>(nameof(TintLuminosityOpacity), 0.16, coerce: (_, value) => Math.Clamp(value, 0, 1));
    public static readonly StyledProperty<double> BlurAmountProperty =
        AvaloniaProperty.Register<AcrylicMaterial, double>(nameof(BlurAmount), 30, coerce: (_, value) => Math.Clamp(value, 0, 250));
    public static readonly StyledProperty<double> SaturationProperty =
        AvaloniaProperty.Register<AcrylicMaterial, double>(nameof(Saturation), 1.25, coerce: (_, value) => Math.Clamp(value, 0, 8));
    public static readonly StyledProperty<double> NoiseOpacityProperty =
        AvaloniaProperty.Register<AcrylicMaterial, double>(nameof(NoiseOpacity), 0.025, coerce: (_, value) => Math.Clamp(value, 0, 1));

    public Color TintColor { get => GetValue(TintColorProperty); set => SetValue(TintColorProperty, value); }
    public double TintOpacity { get => GetValue(TintOpacityProperty); set => SetValue(TintOpacityProperty, value); }
    public double TintLuminosityOpacity { get => GetValue(TintLuminosityOpacityProperty); set => SetValue(TintLuminosityOpacityProperty, value); }
    public double BlurAmount { get => GetValue(BlurAmountProperty); set => SetValue(BlurAmountProperty, value); }
    public double Saturation { get => GetValue(SaturationProperty); set => SetValue(SaturationProperty, value); }
    public double NoiseOpacity { get => GetValue(NoiseOpacityProperty); set => SetValue(NoiseOpacityProperty, value); }
}

public enum MicaThemeMode
{
    Auto,
    Light,
    Dark,
}

public sealed class MicaMaterial : CompositionMaterial
{
    public static readonly StyledProperty<MicaThemeMode> ThemeModeProperty =
        AvaloniaProperty.Register<MicaMaterial, MicaThemeMode>(nameof(ThemeMode));
    public static readonly StyledProperty<Color> TintColorProperty =
        AvaloniaProperty.Register<MicaMaterial, Color>(nameof(TintColor), Colors.Transparent);
    public static readonly StyledProperty<double> TintOpacityProperty =
        AvaloniaProperty.Register<MicaMaterial, double>(nameof(TintOpacity), 0, coerce: (_, value) => Math.Clamp(value, 0, 1));

    public MicaThemeMode ThemeMode { get => GetValue(ThemeModeProperty); set => SetValue(ThemeModeProperty, value); }
    public Color TintColor { get => GetValue(TintColorProperty); set => SetValue(TintColorProperty, value); }
    public double TintOpacity { get => GetValue(TintOpacityProperty); set => SetValue(TintOpacityProperty, value); }
}

public sealed class LiquidGlassMaterial : CompositionMaterial
{
    public static readonly StyledProperty<Color> TintColorProperty =
        AvaloniaProperty.Register<LiquidGlassMaterial, Color>(nameof(TintColor), Color.FromArgb(30, 255, 255, 255));
    public static readonly StyledProperty<double> BlurAmountProperty =
        AcrylicMaterial.BlurAmountProperty.AddOwner<LiquidGlassMaterial>(new StyledPropertyMetadata<double>(22));
    public static readonly StyledProperty<double> SaturationProperty =
        AcrylicMaterial.SaturationProperty.AddOwner<LiquidGlassMaterial>(new StyledPropertyMetadata<double>(1.6));
    public static readonly StyledProperty<Color> HighlightColorProperty =
        AvaloniaProperty.Register<LiquidGlassMaterial, Color>(nameof(HighlightColor), Color.FromArgb(150, 255, 255, 255));
    public static readonly StyledProperty<double> HighlightIntensityProperty =
        AvaloniaProperty.Register<LiquidGlassMaterial, double>(nameof(HighlightIntensity), 0.75, coerce: (_, value) => Math.Clamp(value, 0, 1));
    public static readonly StyledProperty<double> BorderThicknessProperty =
        AvaloniaProperty.Register<LiquidGlassMaterial, double>(nameof(BorderThickness), 1.25, coerce: (_, value) => Math.Max(0, value));
    public static readonly StyledProperty<double> SurfaceOpacityProperty =
        AvaloniaProperty.Register<LiquidGlassMaterial, double>(nameof(SurfaceOpacity), 0.32, coerce: (_, value) => Math.Clamp(value, 0, 1));
    public static readonly StyledProperty<double> FrostedOpacityProperty =
        AvaloniaProperty.Register<LiquidGlassMaterial, double>(nameof(FrostedOpacity), 0.18, coerce: (_, value) => Math.Clamp(value, 0, 1));
    public static readonly StyledProperty<double> EdgeIntensityProperty =
        AvaloniaProperty.Register<LiquidGlassMaterial, double>(nameof(EdgeIntensity), 0.95, coerce: (_, value) => Math.Clamp(value, 0, 1));
    public static readonly StyledProperty<double> EdgeDepthProperty =
        AvaloniaProperty.Register<LiquidGlassMaterial, double>(nameof(EdgeDepth), 5, coerce: (_, value) => Math.Max(0, value));
    public static readonly StyledProperty<double> ChromaticAberrationProperty =
        AvaloniaProperty.Register<LiquidGlassMaterial, double>(nameof(ChromaticAberration), 0.38, coerce: (_, value) => Math.Clamp(value, 0, 1));
    public static readonly StyledProperty<double> PointerGlowRadiusProperty =
        AvaloniaProperty.Register<LiquidGlassMaterial, double>(nameof(PointerGlowRadius), 145, coerce: (_, value) => Math.Max(1, value));

    public Color TintColor { get => GetValue(TintColorProperty); set => SetValue(TintColorProperty, value); }
    public double BlurAmount { get => GetValue(BlurAmountProperty); set => SetValue(BlurAmountProperty, value); }
    public double Saturation { get => GetValue(SaturationProperty); set => SetValue(SaturationProperty, value); }
    public Color HighlightColor { get => GetValue(HighlightColorProperty); set => SetValue(HighlightColorProperty, value); }
    public double HighlightIntensity { get => GetValue(HighlightIntensityProperty); set => SetValue(HighlightIntensityProperty, value); }
    public double BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    public double SurfaceOpacity { get => GetValue(SurfaceOpacityProperty); set => SetValue(SurfaceOpacityProperty, value); }
    public double FrostedOpacity { get => GetValue(FrostedOpacityProperty); set => SetValue(FrostedOpacityProperty, value); }
    public double EdgeIntensity { get => GetValue(EdgeIntensityProperty); set => SetValue(EdgeIntensityProperty, value); }
    public double EdgeDepth { get => GetValue(EdgeDepthProperty); set => SetValue(EdgeDepthProperty, value); }
    public double ChromaticAberration { get => GetValue(ChromaticAberrationProperty); set => SetValue(ChromaticAberrationProperty, value); }
    public double PointerGlowRadius { get => GetValue(PointerGlowRadiusProperty); set => SetValue(PointerGlowRadiusProperty, value); }
}

public sealed class RevealMaterial : CompositionMaterial
{
    public static readonly StyledProperty<CompositionMaterial?> BaseMaterialProperty =
        AvaloniaProperty.Register<RevealMaterial, CompositionMaterial?>(nameof(BaseMaterial));
    public static readonly StyledProperty<Color> HighlightColorProperty =
        AvaloniaProperty.Register<RevealMaterial, Color>(nameof(HighlightColor), Color.FromArgb(115, 255, 255, 255));
    public static readonly StyledProperty<double> RadiusProperty =
        AvaloniaProperty.Register<RevealMaterial, double>(nameof(Radius), 110, coerce: (_, value) => Math.Max(1, value));
    public static readonly StyledProperty<double> FillIntensityProperty =
        AvaloniaProperty.Register<RevealMaterial, double>(nameof(FillIntensity), 0.22, coerce: (_, value) => Math.Clamp(value, 0, 1));
    public static readonly StyledProperty<double> BorderIntensityProperty =
        AvaloniaProperty.Register<RevealMaterial, double>(nameof(BorderIntensity), 0.9, coerce: (_, value) => Math.Clamp(value, 0, 1));
    public static readonly StyledProperty<double> BorderThicknessProperty =
        AvaloniaProperty.Register<RevealMaterial, double>(nameof(BorderThickness), 1, coerce: (_, value) => Math.Max(0, value));

    public CompositionMaterial? BaseMaterial { get => GetValue(BaseMaterialProperty); set => SetValue(BaseMaterialProperty, value); }
    public Color HighlightColor { get => GetValue(HighlightColorProperty); set => SetValue(HighlightColorProperty, value); }
    public double Radius { get => GetValue(RadiusProperty); set => SetValue(RadiusProperty, value); }
    public double FillIntensity { get => GetValue(FillIntensityProperty); set => SetValue(FillIntensityProperty, value); }
    public double BorderIntensity { get => GetValue(BorderIntensityProperty); set => SetValue(BorderIntensityProperty, value); }
    public double BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
}
