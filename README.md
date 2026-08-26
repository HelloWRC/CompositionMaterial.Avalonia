# CompositionMaterial.Avalonia

`CompositionMaterial.Avalonia` is a `net8.0` Avalonia 12.1.1 control library for shape-clipped Windows materials. On Windows it inserts material visuals into Avalonia's existing `Windows.UI.Composition` tree; elsewhere, or when that backend is unavailable, it draws the configured Avalonia fallback brush.

## Requirements

- Avalonia and Avalonia.Win32 **12.1.1 exactly**.
- Windows with `Win32CompositionMode.WinUIComposition` for native materials.
- A transparent window and transparent Avalonia ancestors behind each material region.
- NativeAOT is not supported in this release. Trimmed desktop builds are supported through the included linker descriptor.

On Windows 11 the library acquires `DWMWA_USE_HOSTBACKDROPBRUSH` while its native TopLevel host is alive and restores the window's previous value during disposal. Applications can therefore keep `TransparencyLevelHint="Transparent"`; a global window-wide Acrylic backdrop is not required.

Configure the Windows backend when building the application:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .With(new Win32PlatformOptions
    {
        CompositionMode =
        [
            Win32CompositionMode.WinUIComposition,
            Win32CompositionMode.RedirectionSurface,
        ],
    });
```

The containing window must leave the native layer visible:

```xml
<Window Background="Transparent"
        TransparencyLevelHint="Transparent">
```

## Preset material

```xml
<cm:CompositionMaterialControl
    CornerRadius="24,10,24,10"
    Padding="18"
    FallbackBrush="#CC282C34">
  <cm:CompositionMaterialControl.Material>
    <materials:AcrylicMaterial
        TintColor="#20242C"
        TintOpacity="0.68"
        BlurAmount="30"
        Saturation="1.25" />
  </cm:CompositionMaterialControl.Material>

  <TextBlock Text="Content stays above the native material" />
</cm:CompositionMaterialControl>
```

Also included:

- `MicaMaterial`
- `LiquidGlassMaterial`, an approximation using blur, tint, noise and edge highlights
- `RevealMaterial`, with a pointer-following Fluent 1 style fill and border highlight

Liquid glass appearance can be tuned independently from Acrylic:

```xml
<materials:LiquidGlassMaterial
    TintColor="#1EFFFFFF"
    SurfaceOpacity="0.32"
    FrostedOpacity="0.18"
    HighlightColor="#BEFFFFFF"
    HighlightIntensity="0.75"
    EdgeIntensity="0.95"
    EdgeDepth="5"
    ChromaticAberration="0.38"
    PointerGlowRadius="145"
    BorderThickness="1.25" />
```

LiquidGlass uses a clear native backdrop plus a low-opacity Acrylic frost layer. It renders one thick chromatic edge rim; interior highlights are disabled until the pointer enters the control. Reveal and LiquidGlass pointer lights use absolute X/Y radii, so the highlight remains circular on rectangular controls.

## Custom material graph

```xml
<cm:CompositionMaterialControl FallbackBrush="#CC20242A" CornerRadius="18">
  <cm:CompositionMaterialControl.Material>
    <materials:CustomCompositionMaterial>
      <materials:TintBrushNode Color="#6688CC" Opacity="0.4">
        <materials:GaussianBlurBrushNode Amount="24">
          <materials:BackdropBrushNode />
        </materials:GaussianBlurBrushNode>
      </materials:TintBrushNode>
    </materials:CustomCompositionMaterial>
  </cm:CompositionMaterialControl.Material>
</cm:CompositionMaterialControl>
```

The graph supports backdrop, color, linear/radial gradients, deterministic noise, blur, saturation, opacity, tint, blend, composite and mask nodes. Backdrop and blur are hosted in Windows Composition. Portable color/gradient/noise/mask overlays are drawn by Avalonia above that backdrop so unsupported Direct2D graphs do not disable the entire material.

Avalonia 12.1.1's private WinUI bridge exposes its validated system Acrylic recipe rather than animatable arbitrary Win2D effect properties. `BlurAmount`/`Saturation` select and describe that recipe, but the exact native blur kernel remains controlled by the pinned system implementation.

## Composition animation tracking

No library-specific animation API is required. Direct Avalonia Composition animations are followed automatically:

```csharp
var visual = ElementComposition.GetElementVisual(materialControl)!;
var scale = visual.Compositor.CreateVector3DKeyFrameAnimation();
scale.Duration = TimeSpan.FromSeconds(1.5);
scale.IterationBehavior = AnimationIterationBehavior.Forever;
scale.Direction = PlaybackDirection.Alternate;
scale.InsertKeyFrame(0, new Vector3D(0.94, 0.94, 1));
scale.InsertKeyFrame(1, new Vector3D(1.04, 1.04, 1));
visual.StartAnimation("Scale", scale);
```

Implicit animations, including `this.FinalValue`, are also followed. The library reads the already-evaluated server-side transform after Avalonia's animation pass and applies it to the native material visual before the target is rendered.

Set `EnableLiveTransformTracking="False"` to update only around commits/layout changes for diagnostics or reduced background activity.

`ActualRenderingMode` and `IsNativeMaterialActive` report whether the control is using WinUI Composition or `FallbackBrush`. Set `COMPOSITION_MATERIAL_DIAGNOSTICS=1` before launching an app to emit backend diagnostics to standard error.

## Limitations

- The material samples the desktop/window backdrop, not Avalonia content in the same swapchain.
- An opaque Avalonia parent covers the native material; the library cannot punch a transparent hole through existing pixels.
- Per-corner values are accepted, but the native and child clip currently use the largest radius as a uniform conservative clip on the pinned Avalonia backend.
- Liquid glass does not implement true displacement/refraction or chromatic dispersion.
- Private rendering lifecycle access is guarded by an exact Avalonia 12.1.1 ABI contract. A mismatch safely uses `FallbackBrush`.

Run the sample with:

```powershell
dotnet run --project CompositionMaterial.Avalonia.Demo
```

Run tests with:

```powershell
dotnet test CompositionMaterial.Avalonia.Tests
```
