using System.Numerics;
using Avalonia;
using Avalonia.Media;
using CompositionMaterial.Avalonia.Platform.Windows;
using Xunit;

namespace CompositionMaterial.Avalonia.Tests;

[Collection(AvaloniaTestCollection.Name)]
public class NativeGeometryTests(AvaloniaTestFixture fixture)
{
    [Fact]
    public async Task Native_clip_preserves_the_tip_and_excludes_the_bounding_rectangle()
    {
        if (!OperatingSystem.IsWindows()) return;
        await fixture.RunAsync(() =>
        {
            var body = new RectangleGeometry(new Rect(0, 10, 325, 40), 8, 8);
            var tip = Geometry.Parse("M 152.5,10 L 162.5,0 L 172.5,10 Z");
            var clip = new CombinedGeometry(GeometryCombineMode.Union, body, tip);
            using var path = NativePathClip.GetFillPath(clip);
            using var native = Direct2DPath.Create(path, 1);
            Assert.True(Direct2DPath.Contains(native.DangerousGetHandle(), new Vector2(162.5f, 5)));
            Assert.True(Direct2DPath.Contains(native.DangerousGetHandle(), new Vector2(162.5f, 30)));
            Assert.False(Direct2DPath.Contains(native.DangerousGetHandle(), new Vector2(20, 5)));
            Assert.False(Direct2DPath.Contains(native.DangerousGetHandle(), new Vector2(1, 11)));
        });
    }

    [Fact]
    public async Task Native_clip_matches_transformed_curves_at_monitor_scaling()
    {
        if (!OperatingSystem.IsWindows()) return;
        await fixture.RunAsync(() =>
        {
            var clip = Geometry.Parse("M 0,20 C 0,0 40,0 40,20 Q 20,50 0,20 Z");
            clip.Transform = new TranslateTransform(12, 7);
            using var path = NativePathClip.GetFillPath(clip);
            using var native = Direct2DPath.Create(path, 1.5f);
            for (var y = 0; y < 55; y += 3)
                for (var x = 0; x < 60; x += 3)
                {
                    var point = new Point(x + 0.2, y + 0.3);
                    var expected = clip.FillContains(point);
                    var actual = Direct2DPath.Contains(native.DangerousGetHandle(),
                        new Vector2((float)(point.X * 1.5), (float)(point.Y * 1.5)));
                    Assert.True(expected == actual, $"Point {point}: expected={expected}, native={actual}");
                }
        });
    }

    [Fact]
    public Task Explicit_clip_is_not_overwritten_by_corner_radius_or_bounds_updates() => fixture.RunAsync(() =>
    {
        var clip = new RectangleGeometry(new Rect(10, 10, 30, 20));
        var control = new CompositionMaterialControl { Clip = clip, CornerRadius = new CornerRadius(12) };
        control.CornerRadius = new CornerRadius(20);
        Assert.Same(clip, control.Clip);
        Assert.Same(clip, control.NativeClipGeometry);
    });
}
