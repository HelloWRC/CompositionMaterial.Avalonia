> [!WARNING]
> **本项目尚处于实验阶段，且仅支持 Windows。** 项目依赖 Avalonia 的内部实现，因此 Avalonia 的任何内部变更都可能随时导致部分或全部功能失效。请勿将当前行为或兼容性视为稳定承诺，也不建议在生产环境中使用。

# CompositionMaterial.Avalonia

[English](README.md) | 简体中文

![demo](https://res.classisland.tech/screenshots/CompositionMaterial.Avalonia/1.webp)

`CompositionMaterial.Avalonia` 是一个面向 `net8.0` 的 Avalonia 控件库，可在 Avalonia 应用中呈现按控件形状裁剪的 Windows 材质。它会将材质视觉对象（Visual）插入 Avalonia 现有的 `Windows.UI.Composition` 视觉树，并将常规 Avalonia 内容显示在材质之上。

与在应用内手动采集窗口后界面并进行合成的方案相比，此方案直接使用了系统的效果接口，一般情况下性能更好。

## 功能

- 提供 `CompositionMaterialControl` 装饰器，支持 `Padding`、`CornerRadius`、形状裁剪和 Avalonia `FallbackBrush`。
- 内置 Acrylic、Mica、Liquid Glass 和 Fluent 1 Reveal 材质。
- 支持自定义材质图，可组合背景、纯色、线性渐变、径向渐变、确定性噪声、模糊、饱和度、不透明度、着色、混合、合成及蒙版节点。
- 自动跟踪 Avalonia Composition 的直接动画与隐式动画。
- 可通过 `ActualRenderingMode` 和 `IsNativeMaterialActive` 获取运行时渲染后端的状态。
- 提供诊断输出；无法附加原生后端时，可安全回退。

Liquid Glass 通过模糊、着色、噪声、边缘高光和指针光效模拟玻璃质感。Reveal 则提供跟随指针移动的填充与边框高光。

## 要求与兼容性

- **仅支持 Windows。** 回退路径仅用于故障保护，不代表项目支持跨平台运行。
- .NET 8 SDK，以及以 `net8.0` 为目标框架的应用。
- **必须精确使用** Avalonia 和 Avalonia.Win32 12.1.1。
- 每个材质区域所在的窗口，以及该区域后方的所有 Avalonia 祖先元素，都必须保持透明。
- 不支持 NativeAOT；经裁剪的桌面应用则可通过内置的链接器描述文件获得支持。

在较新版本的 Windows 上，Avalonia 会自动选择 WinUI Composition 后端，无需显式配置 `Win32PlatformOptions` 或 `CompositionMode`。

在 Windows 11 上，只要原生 TopLevel 材质宿主仍然存在，本库就会启用 `DWMWA_USE_HOSTBACKDROPBRUSH`；宿主释放后，则恢复窗口的原始设置。因此，无需为整个窗口启用全局 Acrylic 背景。

## 用法

### 1. 通过 NuGet 安装

通过 NuGet 安装包：

```powershell
dotnet add package CompositionMaterial.Avalonia --version 0.1.0
```

也可以直接在应用项目中添加包引用：

```xml
<ItemGroup>
  <PackageReference Include="CompositionMaterial.Avalonia" Version="0.1.0" />
</ItemGroup>
```

### 2. 让原生图层保持可见

包含材质的窗口，以及材质区域后方的所有 Avalonia 元素，都必须保持透明：

```xml
<Window Background="Transparent"
        TransparencyLevelHint="Transparent">
```

任何不透明的 Avalonia 祖先元素都会遮住原生材质。

### 3. 添加预设材质

声明控件与材质命名空间，然后使用该控件包裹需要显示在材质上方的内容：

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

    <TextBlock Text="内容显示在原生材质上方" />
  </cm:CompositionMaterialControl>
</Window>
```

可用的预设材质：

- `AcrylicMaterial`：可调整着色、明度、系统模糊配方、饱和度和噪声。
- `MicaMaterial`：支持自动、浅色或深色主题模式，以及可选着色。
- `LiquidGlassMaterial`：可调整表面与磨砂层的不透明度、高光、边缘深度、色差边缘偏移和指针光效。
- `RevealMaterial`：在可选的基础材质上叠加跟随指针的填充和边框高光。

Liquid Glass 参数示例：

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

Liquid Glass 使用透明的原生背景，并叠加低不透明度的 Acrylic 磨砂层。内部高光仅在指针进入控件后显示。Reveal 与 Liquid Glass 均使用绝对 X/Y 半径，因此即使控件为矩形，指针光效仍会保持圆形。

## 自定义材质图

如果预设材质无法满足需求，可以使用 `CustomCompositionMaterial`：

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

背景与模糊节点由 Windows Composition 承载；可移植的纯色、渐变、噪声和蒙版叠加层则由 Avalonia 绘制在背景之上。因此，即使材质图包含 Direct2D 不支持的部分，也不会导致整个材质被禁用。循环引用的材质图将被拒绝。

## 动画跟踪与诊断

无需使用本库专有的动画 API。Avalonia Composition 的直接动画会被自动跟踪：

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

包括 `this.FinalValue` 在内的隐式动画也会被跟踪。本库会在 Avalonia 动画阶段结束后读取已完成求值的服务端变换，并在渲染前将其应用到原生视觉对象（Visual）。

- 将 `EnableLiveTransformTracking` 设为 `False` 后，仅会在提交及布局变更前后更新变换。这可用于诊断问题或减少后台活动。
- 读取 `ActualRenderingMode` 或 `IsNativeMaterialActive`，即可判断当前使用的是 WinUI Composition 还是 `FallbackBrush`。
- 启动前将环境变量 `COMPOSITION_MATERIAL_DIAGNOSTICS` 设为 `1`，即可将后端诊断信息输出至标准错误流。

## 限制

- 本项目依赖 Avalonia 12.1.1 的私有渲染生命周期及 WinUI 桥接细节。Avalonia 的任何内部变更都可能破坏部分或全部功能。
- 仅支持 Windows。在非 Windows 环境中，或原生后端不可用、ABI 不匹配时，会尽可能回退至 `FallbackBrush`。
- 材质采样的是桌面/窗口背景，而不是同一个交换链中的 Avalonia 内容。
- 不透明的 Avalonia 父元素会遮住原生材质；本库无法在 Avalonia 已绘制的像素中打出透明孔洞。
- 可以分别为四个角传入不同的圆角值，但当前固定版本后端的原生裁剪与子元素裁剪会取其中的最大值，作为统一且保守的圆角值。
- Liquid Glass 不实现真实的位移、折射或色散。
- Avalonia 12.1.1 暴露的是经过验证的系统 Acrylic 配方，而非可任意动画化的 Win2D 效果属性。`BlurAmount` 和 `Saturation` 用于选择并描述该配方，但具体的原生模糊核仍由固定版本的 Avalonia 实现控制。
- 不支持 NativeAOT。

## 开发

仓库结构：

- `CompositionMaterial.Avalonia`：控件库和 Windows 后端。
- `CompositionMaterial.Avalonia.Demo`：可交互的预设材质与自定义材质演示，以及参数编辑器。
- `CompositionMaterial.Avalonia.Tests`：API、材质图、变换、XAML 和私有 ABI 契约测试。

请在 Windows 上使用 .NET 8 SDK：

```powershell
dotnet restore CompositionMaterial.Avalonia.sln
dotnet build CompositionMaterial.Avalonia.sln
dotnet test CompositionMaterial.Avalonia.Tests
dotnet run --project CompositionMaterial.Avalonia.Demo
```

运行原生后端冒烟测试。只有 Demo 中的所有卡片都成功附加到 WinUI Composition 后，进程才会以成功状态退出：

```powershell
dotnet run --project CompositionMaterial.Avalonia.Demo -- --smoke-test
```

构建本地包：

```powershell
dotnet pack CompositionMaterial.Avalonia\CompositionMaterial.Avalonia.csproj -c Release
```

更改 Avalonia 版本或原生后端后，必须重新验证精确 ABI 契约、附加与释放流程、动画同步、裁剪发布行为及回退行为。

## 许可

本项目采用 [MIT License](LICENSE.txt)。软件按「原样」提供，不附带任何保证；完整条款请参阅许可证原文。
