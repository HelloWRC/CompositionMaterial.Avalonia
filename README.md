> [!WARNING]
> **Experimental and Windows-only.** This project depends on Avalonia internals, so any feature may break without warning when those internals change. Do not rely on its current behavior or compatibility in production.

# CompositionMaterial.Avalonia

English | [简体中文](README.zh-CN.md)

![demo](https://res.classisland.tech/screenshots/CompositionMaterial.Avalonia/1.webp)

`CompositionMaterial.Avalonia` is a `net8.0` control library that adds shape-clipped Windows materials to Avalonia applications. It injects material visuals into Avalonia's existing `Windows.UI.Composition` tree and keeps normal Avalonia content above them.

Compared to approaches that manually capture the window backdrop and composite it inside the app, this approach talks to the system's effect interfaces directly and generally performs better.

## Features

- A `CompositionMaterialControl` decorator with `Padding`, `CornerRadius`, clipping, and an Avalonia `FallbackBrush`.
- Preset Acrylic, Mica, Liquid Glass, and Fluent 1 Reveal materials.
- Custom material graphs composed from backdrop, color, linear/radial gradient, deterministic noise, blur, saturation, opacity, tint, blend, composite, and mask nodes.
- Automatic tracking of direct and implicit Avalonia Composition animations.
- Runtime backend state through `ActualRenderingMode` and `IsNativeMaterialActive`.
- Diagnostic output, plus a safe fallback path when the native backend cannot be attached.

Liquid Glass is an approximation built from blur, tint, noise, edge highlights, and pointer lighting. Reveal adds a pointer-following fill and border highlight.

## Requirements and compatibility

- **Windows only.** The fallback path is a safety net, not a supported cross-platform implementation.
- The .NET 8 SDK and a `net8.0` application.
- **Exactly** Avalonia and Avalonia.Win32 12.1.1.
- A transparent window and transparent Avalonia ancestors behind every material region.
- NativeAOT is not supported. Trimmed desktop builds are supported through the included linker descriptor.

On newer Windows versions, Avalonia automatically selects the WinUIComposition backend. No explicit `Win32PlatformOptions` or `CompositionMode` configuration is required.

On Windows 11, the library enables `DWMWA_USE_HOSTBACKDROPBRUSH` while a native TopLevel material host is alive and restores the window's previous value when that host is disposed. A global, window-wide Acrylic backdrop is therefore unnecessary.

## Usage

### 1. Install from NuGet

Install the package from NuGet:

```powershell
dotnet add package CompositionMaterial.Avalonia --version 0.1.0
```

Or add the package reference directly to the application project:

```xml
<ItemGroup>
  <PackageReference Include="CompositionMaterial.Avalonia" Version="0.1.0" />
</ItemGroup>
```

### 2. Keep the native layer visible

The containing window and all Avalonia elements behind a material region must be transparent:

```xml
<Window Background="Transparent"
        TransparencyLevelHint="Transparent">
```

An opaque Avalonia ancestor will cover the native material.

### 3. Add a preset material

Declare the control and material namespaces, then wrap the content that should appear above the material:

```xml
<Window
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:cm="using:CompositionMaterial.Avalonia"
    xmlns:materials="using:CompositionMaterial.Avalonia.Materials">

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
</Window>
```

Available presets:

- `AcrylicMaterial`: tint, luminosity, system blur recipe, saturation, and noise controls.
- `MicaMaterial`: automatic, light, or dark theme mode with optional tint.
- `LiquidGlassMaterial`: tunable surface/frost opacity, highlights, edge depth, chromatic edge offset, and pointer glow.
- `RevealMaterial`: a pointer-following fill and border over an optional base material.

Example Liquid Glass tuning:

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

Liquid Glass uses a clear native backdrop with a low-opacity Acrylic frost layer, and its interior highlights appear once the pointer enters the control. Reveal and Liquid Glass use absolute X/Y radii so their pointer lights stay circular on rectangular controls.

## Custom material graphs

Use `CustomCompositionMaterial` when a preset is not enough:

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

Backdrop and blur nodes are hosted in Windows Composition. Portable color, gradient, noise, and mask overlays are drawn by Avalonia above the backdrop, so an unsupported Direct2D graph does not disable the whole material. Cyclic graphs are rejected.

## Animation tracking and diagnostics

No library-specific animation API is required. Direct Avalonia Composition animations are tracked automatically:

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

Implicit animations, including `this.FinalValue`, are tracked as well. The library reads the evaluated server-side transform after Avalonia's animation pass and applies it to the native visual before rendering.

- Set `EnableLiveTransformTracking="False"` to update transforms only around commits and layout changes. This is useful for diagnostics or reducing background activity.
- Read `ActualRenderingMode` or `IsNativeMaterialActive` to see whether WinUI Composition or `FallbackBrush` is active.
- Set the `COMPOSITION_MATERIAL_DIAGNOSTICS=1` environment variable before launch to write backend diagnostics to standard error.

## Limitations

- The project relies on private Avalonia 12.1.1 rendering lifecycle and WinUI bridge details. Any Avalonia internal change can break some or all functionality.
- Only Windows is supported. A non-Windows environment, or an unavailable or mismatched native backend, falls back to `FallbackBrush` where possible.
- The material samples the desktop/window backdrop, not Avalonia content in the same swapchain.
- An opaque Avalonia parent covers the native material; the library cannot punch a transparent hole through pixels Avalonia has already rendered.
- Per-corner radii are accepted, but the native and child clips currently use the largest value as a uniform conservative radius on the pinned backend.
- Liquid Glass does not implement real displacement, refraction, or chromatic dispersion.
- Avalonia 12.1.1 exposes its validated system Acrylic recipe rather than arbitrary animatable Win2D effect properties. `BlurAmount` and `Saturation` select and describe that recipe, but the exact native blur kernel remains controlled by Avalonia's pinned implementation.
- NativeAOT is unsupported.

## Development

The repository contains:

- `CompositionMaterial.Avalonia`: the control library and Windows backend.
- `CompositionMaterial.Avalonia.Demo`: an interactive preset/custom-material gallery and parameter editor.
- `CompositionMaterial.Avalonia.Tests`: API, graph, transform, XAML, and private ABI contract tests.

Use the .NET 8 SDK on Windows:

```powershell
dotnet restore CompositionMaterial.Avalonia.sln
dotnet build CompositionMaterial.Avalonia.sln
dotnet test CompositionMaterial.Avalonia.Tests
dotnet run --project CompositionMaterial.Avalonia.Demo
```

Run the native backend smoke test, which exits successfully only when all demo cards attach to WinUI Composition:

```powershell
dotnet run --project CompositionMaterial.Avalonia.Demo -- --smoke-test
```

Build a local package with:

```powershell
dotnet pack CompositionMaterial.Avalonia\CompositionMaterial.Avalonia.csproj -c Release
```

When changing the Avalonia version or the native backend, treat the exact ABI contract, attachment/disposal path, animation synchronization, trimming behavior, and fallback behavior as required regression areas.

## License

This project is licensed under the [MIT License](LICENSE.txt). The software is provided "as is", without warranty; see the license text for the complete terms.
