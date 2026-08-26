using Avalonia;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using CompositionMaterial.Avalonia.Internal;
using CompositionMaterial.Avalonia.Materials;
using CompositionMaterial.Avalonia.Platform.Windows;
using Xunit;

namespace CompositionMaterial.Avalonia.Tests;

public sealed class MaterialApiTests
{
    [Fact]
    public void Control_starts_in_fallback_mode()
    {
        var control = new CompositionMaterialControl
        {
            FallbackBrush = Brushes.Red,
            CornerRadius = new CornerRadius(12),
        };

        Assert.Equal(MaterialRenderingMode.Fallback, control.ActualRenderingMode);
        Assert.False(control.IsNativeMaterialActive);
        Assert.Same(Brushes.Red, control.FallbackBrush);
    }

    [Fact]
    public void Preset_values_are_coerced_to_safe_ranges()
    {
        var acrylic = new AcrylicMaterial
        {
            TintOpacity = 4,
            NoiseOpacity = -1,
            BlurAmount = 900,
        };

        Assert.Equal(1, acrylic.TintOpacity);
        Assert.Equal(0, acrylic.NoiseOpacity);
        Assert.Equal(250, acrylic.BlurAmount);

        var liquid = new LiquidGlassMaterial
        {
            SurfaceOpacity = 2,
            FrostedOpacity = -2,
            EdgeIntensity = -1,
            EdgeDepth = -4,
            ChromaticAberration = 3,
        };
        Assert.Equal(1, liquid.SurfaceOpacity);
        Assert.Equal(0, liquid.FrostedOpacity);
        Assert.Equal(0, liquid.EdgeIntensity);
        Assert.Equal(0, liquid.EdgeDepth);
        Assert.Equal(1, liquid.ChromaticAberration);
    }

    [Fact]
    public void Graph_validator_detects_cycles()
    {
        var blur = new GaussianBlurBrushNode();
        blur.Source = blur;

        var exception = Assert.Throws<InvalidOperationException>(() => MaterialGraph.Validate(blur));
        Assert.Contains("cycle", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Graph_search_finds_nested_backdrop_and_blur()
    {
        var graph = new TintBrushNode
        {
            Source = new GaussianBlurBrushNode
            {
                Source = new BackdropBrushNode { Kind = BackdropKind.HostBackdrop },
            },
        };

        Assert.True(MaterialGraph.Contains<GaussianBlurBrushNode>(graph));
        Assert.Equal(BackdropKind.HostBackdrop, MaterialGraph.FindBackdrop(graph));
    }

    [Fact]
    public void Nested_graph_changes_are_observed()
    {
        var tint = new TintBrushNode { Source = new BackdropBrushNode() };
        var material = new CustomCompositionMaterial { RootBrush = tint };
        var changes = 0;
        using var observer = new MaterialGraphObserver(material, (_, _) => changes++);

        tint.Opacity = 0.25;

        Assert.Equal(1, changes);
    }

    [Fact]
    public void Dip_translation_is_scaled_to_native_pixels()
    {
        var matrix = Matrix.CreateTranslation(12.5, -4.25) * Matrix.CreateScale(1.2, 0.8);
        var native = NativeTransformMath.ToPixels(matrix, 1.5);

        Assert.Equal((float)matrix.M11, native.M11, 4);
        Assert.Equal((float)matrix.M22, native.M22, 4);
        Assert.Equal((float)(matrix.M31 * 1.5), native.M41, 4);
        Assert.Equal((float)(matrix.M32 * 1.5), native.M42, 4);
    }

    [Fact]
    public void Avalonia_1211_private_contract_is_resolvable()
    {
        Assert.NotNull(WinUiAbi.TryCreate());
    }

    [Fact]
    public void Custom_material_can_be_declared_in_xaml()
    {
        const string xaml = """
            <cm:CompositionMaterialControl
                xmlns="https://github.com/avaloniaui"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:cm="using:CompositionMaterial.Avalonia"
                xmlns:m="using:CompositionMaterial.Avalonia.Materials"
                CornerRadius="18" FallbackBrush="#CC20242A">
              <cm:CompositionMaterialControl.Material>
                <m:CustomCompositionMaterial>
                  <m:TintBrushNode Color="#6688CC" Opacity="0.4">
                    <m:GaussianBlurBrushNode Amount="24">
                      <m:BackdropBrushNode />
                    </m:GaussianBlurBrushNode>
                  </m:TintBrushNode>
                </m:CustomCompositionMaterial>
              </cm:CompositionMaterialControl.Material>
            </cm:CompositionMaterialControl>
            """;

        var result = AvaloniaRuntimeXamlLoader.Parse<CompositionMaterialControl>(xaml, typeof(MaterialApiTests).Assembly);

        var custom = Assert.IsType<CustomCompositionMaterial>(result.Material);
        var tint = Assert.IsType<TintBrushNode>(custom.RootBrush);
        Assert.IsType<GaussianBlurBrushNode>(tint.Source);
        Assert.Equal(new CornerRadius(18), result.CornerRadius);
    }
}
