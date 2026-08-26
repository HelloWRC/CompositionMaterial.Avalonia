using Avalonia;
using Avalonia.Media;
using CompositionMaterial.Avalonia.Materials;
using MaterialDefinition = CompositionMaterial.Avalonia.Materials.CompositionMaterial;

namespace CompositionMaterial.Avalonia.Internal;

internal static class MaterialOverlayRenderer
{
    public static void Render(DrawingContext context, Rect bounds, CornerRadius radius, MaterialDefinition? material,
        Point pointer, bool pointerOver)
    {
        DrawBaseTint(context, bounds, radius, material);
        if (material is AcrylicMaterial { NoiseOpacity: > 0 } acrylic)
            DrawNoise(context, bounds, radius, acrylic.NoiseOpacity);

        if (material is CustomCompositionMaterial { RootBrush: { } root })
        {
            DrawCustomNode(context, bounds, radius, root);
            return;
        }

        if (material is RevealMaterial reveal && pointerOver)
        {
            DrawReveal(context, bounds, radius, pointer, reveal);
            return;
        }

        if (material is LiquidGlassMaterial liquid)
            DrawLiquidGlass(context, bounds, radius, pointer, pointerOver, liquid);
    }

    private static void DrawCustomNode(DrawingContext context, Rect bounds, CornerRadius radius, MaterialBrushNode node)
    {
        switch (node)
        {
            case BackdropBrushNode:
                return;
            case ColorBrushNode color:
                context.DrawRectangle(new SolidColorBrush(color.Color), null, new RoundedRect(bounds, radius));
                return;
            case LinearGradientBrushNode linear:
                context.DrawRectangle(CreateLinear(linear), null, new RoundedRect(bounds, radius));
                return;
            case RadialGradientBrushNode radial:
                context.DrawRectangle(CreateRadial(radial), null, new RoundedRect(bounds, radius));
                return;
            case NoiseBrushNode noise:
                DrawNoise(context, bounds, radius, noise.Opacity, noise.Scale);
                return;
            case OpacityBrushNode opacity when opacity.Source is { } opacitySource:
                using (context.PushOpacity(opacity.Opacity))
                    DrawCustomNode(context, bounds, radius, opacitySource);
                return;
            case TintBrushNode tint when tint.Source is { } tintSource:
                DrawCustomNode(context, bounds, radius, tintSource);
                context.DrawRectangle(new SolidColorBrush(WithOpacity(tint.Color, tint.Opacity)), null,
                    new RoundedRect(bounds, radius));
                return;
            case UnaryMaterialBrushNode unary when unary.Source is { } source:
                DrawCustomNode(context, bounds, radius, source);
                return;
            case BlendBrushNode blend:
                if (blend.Background is { } background) DrawCustomNode(context, bounds, radius, background);
                if (blend.Foreground is { } foreground) DrawCustomNode(context, bounds, radius, foreground);
                return;
            case CompositeBrushNode composite:
                foreach (var sourceNode in composite.Sources) DrawCustomNode(context, bounds, radius, sourceNode);
                return;
            case MaskBrushNode mask when mask.Source is { } maskSource:
                if (mask.Mask is { } maskNode && TryCreateBrush(maskNode) is { } maskBrush)
                {
                    using (context.PushOpacityMask(maskBrush, bounds))
                        DrawCustomNode(context, bounds, radius, maskSource);
                }
                else
                {
                    DrawCustomNode(context, bounds, radius, maskSource);
                }
                return;
        }
    }

    private static IBrush? TryCreateBrush(MaterialBrushNode node) => node switch
    {
        ColorBrushNode color => new SolidColorBrush(color.Color),
        LinearGradientBrushNode linear => CreateLinear(linear),
        RadialGradientBrushNode radial => CreateRadial(radial),
        UnaryMaterialBrushNode { Source: { } source } => TryCreateBrush(source),
        _ => null,
    };

    private static void DrawNoise(DrawingContext context, Rect bounds, CornerRadius radius, double opacity, double scale = 1)
    {
        if (opacity <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
            return;
        using var clip = context.PushClip(new RoundedRect(bounds, radius));
        var state = 0x9E3779B9u;
        var count = Math.Clamp((int)(bounds.Width * bounds.Height / 900), 24, 180);
        var pixel = Math.Max(0.55, scale);
        var brush = new SolidColorBrush(Color.FromArgb((byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255), 255, 255, 255));
        for (var index = 0; index < count; index++)
        {
            state = state * 1664525u + 1013904223u;
            var x = bounds.X + state / (double)uint.MaxValue * bounds.Width;
            state = state * 1664525u + 1013904223u;
            var y = bounds.Y + state / (double)uint.MaxValue * bounds.Height;
            context.DrawRectangle(brush, null, new Rect(x, y, pixel, pixel));
        }
    }

    private static LinearGradientBrush CreateLinear(LinearGradientBrushNode node)
    {
        var brush = new LinearGradientBrush { StartPoint = node.StartPoint, EndPoint = node.EndPoint };
        foreach (var stop in node.GradientStops.OrderBy(stop => stop.Offset))
            brush.GradientStops.Add(new GradientStop(stop.Color, stop.Offset));
        return brush;
    }

    private static RadialGradientBrush CreateRadial(RadialGradientBrushNode node)
    {
        var brush = new RadialGradientBrush
        {
            Center = node.Center,
            GradientOrigin = node.Center,
            RadiusX = new RelativeScalar(node.Radius, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(node.Radius, RelativeUnit.Relative),
        };
        foreach (var stop in node.GradientStops.OrderBy(stop => stop.Offset))
            brush.GradientStops.Add(new GradientStop(stop.Color, stop.Offset));
        return brush;
    }

    private static void DrawBaseTint(DrawingContext context, Rect bounds, CornerRadius radius, MaterialDefinition? material)
    {
        if (material is RevealMaterial reveal)
            material = reveal.BaseMaterial;

        Color? color = material switch
        {
            AcrylicMaterial acrylic => WithOpacity(acrylic.TintColor, acrylic.TintOpacity),
            MicaMaterial mica when mica.TintOpacity > 0 => WithOpacity(mica.TintColor, mica.TintOpacity),
            _ => null,
        };
        if (color.HasValue)
            context.DrawRectangle(new SolidColorBrush(color.Value), null, new RoundedRect(bounds, radius));
    }

    private static void DrawReveal(DrawingContext context, Rect bounds, CornerRadius radius, Point pointer,
        RevealMaterial material)
    {
        var fill = CreateRadial(pointer, bounds, material.Radius,
            WithOpacity(material.HighlightColor, material.FillIntensity), Colors.Transparent);
        context.DrawRectangle(fill, null, new RoundedRect(bounds, radius));

        if (material.BorderThickness > 0)
        {
            var border = CreateRadial(pointer, bounds, material.Radius,
                WithOpacity(material.HighlightColor, material.BorderIntensity), Colors.Transparent);
            context.DrawRectangle(null, new Pen(border, material.BorderThickness), new RoundedRect(bounds, radius));
        }
    }

    private static void DrawLiquidGlass(DrawingContext context, Rect bounds, CornerRadius radius, Point pointer,
        bool pointerOver, LiquidGlassMaterial material)
    {
        var shape = new RoundedRect(bounds, radius);

        // Keep the body clear. The lightly frosted native layer supplies readability without hiding the backdrop.
        context.DrawRectangle(new SolidColorBrush(WithOpacity(material.TintColor, material.SurfaceOpacity)), null, shape);

        // Highlights exist only while the pointer is over the glass.
        if (pointerOver)
        {
            var broadGlint = CreateRadial(pointer, bounds, material.PointerGlowRadius,
                WithOpacity(material.HighlightColor, material.HighlightIntensity * 0.14), Colors.Transparent);
            context.DrawRectangle(broadGlint, null, shape);

            var specularGlint = CreateRadial(pointer, bounds, material.PointerGlowRadius * 0.36,
                WithOpacity(material.HighlightColor, material.HighlightIntensity * 0.30), Colors.Transparent);
            context.DrawRectangle(specularGlint, null, shape);

            if (material.ChromaticAberration > 0)
            {
                var shift = 8 + material.ChromaticAberration * 16;
                var cyan = CreateRadial(new Point(pointer.X - shift, pointer.Y), bounds, material.PointerGlowRadius * 0.58,
                    Color.FromArgb((byte)Math.Round(52 * material.ChromaticAberration), 70, 220, 255), Colors.Transparent);
                var magenta = CreateRadial(new Point(pointer.X + shift, pointer.Y), bounds, material.PointerGlowRadius * 0.58,
                    Color.FromArgb((byte)Math.Round(44 * material.ChromaticAberration), 255, 90, 200), Colors.Transparent);
                context.DrawRectangle(cyan, null, shape);
                context.DrawRectangle(magenta, null, shape);
            }
        }

        // One thick refractive rim replaces the previous outer + inner double frame.
        if (material.BorderThickness > 0)
        {
            var refractiveRim = new LinearGradientBrush
            {
                StartPoint = RelativePoint.TopLeft,
                EndPoint = RelativePoint.BottomRight,
                GradientStops =
                {
                    new GradientStop(Color.FromArgb((byte)Math.Round(105 * material.ChromaticAberration), 50, 215, 255), 0),
                    new GradientStop(WithOpacity(material.HighlightColor, material.EdgeIntensity * 0.72), 0.18),
                    new GradientStop(WithOpacity(material.HighlightColor, material.EdgeIntensity * 0.24), 0.48),
                    new GradientStop(Color.FromArgb((byte)Math.Round(58 * material.EdgeIntensity), 0, 0, 0), 0.78),
                    new GradientStop(Color.FromArgb((byte)Math.Round(92 * material.ChromaticAberration), 255, 75, 190), 1),
                },
            };
            var edgeWidth = material.BorderThickness + material.EdgeDepth * 0.35;
            context.DrawRectangle(null, new Pen(refractiveRim, edgeWidth), shape);
        }

        DrawNoise(context, bounds, radius, 0.012, 0.65);
    }

    private static RadialGradientBrush CreateRadial(Point point, Rect bounds, double radius, Color inner, Color outer)
    {
        var x = bounds.Width <= 0 ? 0.5 : point.X / bounds.Width;
        var y = bounds.Height <= 0 ? 0.5 : point.Y / bounds.Height;
        return new RadialGradientBrush
        {
            Center = new RelativePoint(x, y, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(x, y, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(Math.Max(0.001, radius), RelativeUnit.Absolute),
            RadiusY = new RelativeScalar(Math.Max(0.001, radius), RelativeUnit.Absolute),
            GradientStops = { new GradientStop(inner, 0), new GradientStop(outer, 1) },
        };
    }

    private static Color WithOpacity(Color color, double opacity)
        => Color.FromArgb((byte)Math.Clamp(Math.Round(color.A * opacity), 0, 255), color.R, color.G, color.B);
}
