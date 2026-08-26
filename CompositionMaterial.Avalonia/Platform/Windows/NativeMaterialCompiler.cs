using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CompositionMaterial.Avalonia.Internal;
using CompositionMaterial.Avalonia.Materials;
using MaterialDefinition = CompositionMaterial.Avalonia.Materials.CompositionMaterial;

namespace CompositionMaterial.Avalonia.Platform.Windows;

[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Composition methods are preserved by the package descriptor.")]
internal static class NativeMaterialCompiler
{
    public static NativeMaterialDefinition? Compile(NativeWindowContext context, MaterialDefinition? material)
    {
        return material switch
        {
            null => null,
            RevealMaterial reveal => Compile(context, reveal.BaseMaterial ?? new AcrylicMaterial()),
            MicaMaterial mica => Wrap(CompileMica(context, mica)),
            CustomCompositionMaterial { RootBrush: { } root } => CompileCustomBase(context, root),
            AcrylicMaterial => Wrap(CompileSystemAcrylic(context)),
            LiquidGlassMaterial liquid => CompileLiquidGlass(context, liquid),
            _ => null,
        };
    }

    private static NativeMaterialDefinition? CompileCustomBase(NativeWindowContext context, MaterialBrushNode root)
    {
        MaterialGraph.Validate(root);
        if (MaterialGraph.Contains<GaussianBlurBrushNode>(root))
            return Wrap(CompileSystemAcrylic(context));

        return Wrap(CreateBackdropBrush(context, MaterialGraph.FindBackdrop(root) ?? BackdropKind.Backdrop));
    }

    private static NativeMaterialDefinition? CompileLiquidGlass(NativeWindowContext context, LiquidGlassMaterial material)
    {
        object? primary = null;
        object? frost = null;
        try
        {
            primary = CreateBackdropBrush(context, BackdropKind.HostBackdrop);
            frost = CompileSystemAcrylic(context);
            return new NativeMaterialDefinition(primary, frost, (float)material.FrostedOpacity);
        }
        catch
        {
            NativeWindowContext.DisposeProxy(frost);
            NativeWindowContext.DisposeProxy(primary);
            return Wrap(CompileSystemAcrylic(context));
        }
    }

    private static object? CompileSystemAcrylic(NativeWindowContext context)
    {
        var method = context.Abi.WinUiUtilsType.GetMethod("CreateAcrylicBlurBackdropBrush",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return method?.Invoke(null, [context.Compositor]);
    }

    private static object? CompileMica(NativeWindowContext context, MicaMaterial material)
    {
        var dark = material.ThemeMode == MicaThemeMode.Dark ||
                   material.ThemeMode == MicaThemeMode.Auto && context.IsDarkTheme;
        var value = dark ? 32f : 242f;
        var opacity = dark ? 0.8f : 0.6f;
        var method = context.Abi.WinUiUtilsType.GetMethod("CreateMicaBackdropBrush",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var brush = method?.Invoke(null, [context.Compositor, value, opacity]);
        return brush ?? CompileSystemAcrylic(context);
    }

    private static object CreateBackdropBrush(NativeWindowContext context, BackdropKind kind)
    {
        try
        {
            var interfaceName = kind switch
            {
                BackdropKind.HostBackdrop => "Avalonia.Win32.WinRT.ICompositor3",
                BackdropKind.Wallpaper => "Avalonia.Win32.WinRT.ICompositorWithBlurredWallpaperBackdropBrush",
                _ => "Avalonia.Win32.WinRT.ICompositor2",
            };
            var methodName = kind switch
            {
                BackdropKind.HostBackdrop => "CreateHostBackdropBrush",
                BackdropKind.Wallpaper => "TryCreateBlurredWallpaperBackdropBrush",
                _ => "CreateBackdropBrush",
            };
            var interfaceType = context.Abi.RequireWin32Type(interfaceName);
            using var compositor = new ReflectedDisposable(context.Abi.QueryInterface(context.Compositor, interfaceType));
            var raw = WinUiAbi.RequireMethod(interfaceType, methodName, 0).Invoke(compositor.Value, null)
                      ?? throw new InvalidOperationException($"{methodName} returned null.");
            using var rawBrush = new ReflectedDisposable(raw);
            return context.Abi.QueryInterface(raw, context.Abi.ICompositionBrushType);
        }
        catch when (kind != BackdropKind.Backdrop)
        {
            return CreateBackdropBrush(context, BackdropKind.Backdrop);
        }
    }

    private static NativeMaterialDefinition? Wrap(object? brush)
        => brush is null ? null : new NativeMaterialDefinition(brush);

    private readonly struct ReflectedDisposable : IDisposable
    {
        public object? Value { get; }
        public ReflectedDisposable(object? value) => Value = value;
        public void Dispose() => NativeWindowContext.DisposeProxy(Value);
    }
}
