using System.Numerics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using MicroCom.Runtime;
using CompositionMaterial.Avalonia.Internal;
using System.Diagnostics.CodeAnalysis;

namespace CompositionMaterial.Avalonia.Platform.Windows;

/// <summary>
/// Resolves the exact Avalonia 12.1.1 private ABI used by the Windows material host.
/// All failures are converted to an unavailable backend.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The package ships an ILLink descriptor for the pinned Avalonia assemblies.")]
[UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "The exact generic MicroCom interfaces are preserved by the package descriptor.")]
[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The reflected members are preserved by the package descriptor.")]
internal sealed class WinUiAbi
{
    private const string ExpectedVersion = "12.1.1.0";
    private readonly Assembly _win32;
    private readonly Assembly _base;
    private readonly MethodInfo _queryInterface;

    public Type IVisualType { get; }
    public Type IContainerVisualType { get; }
    public Type ICompositionBrushType { get; }
    public Type IGraphicsEffectSourceType { get; }
    public Type IGraphicsEffectType { get; }
    public Type WinUiSurfaceType { get; }
    public Type WinUiWindowType { get; }
    public Type WinUiUtilsType { get; }
    public Type HStringType { get; }
    public Type NativeWinRtMethodsType { get; }

    public Func<object, object?> GetServerVisual { get; }
    public Func<object, Matrix?> GetOwnTransform { get; }
    public Func<object, object?> GetServerParent { get; }
    public Func<object, float> GetServerOpacity { get; }
    public Func<object, bool> GetServerVisible { get; }
    public Func<object, object> GetServerCompositor { get; }
    public Func<object, object> GetServerAnimations { get; }
    public Func<object, bool> GetNeedNextTick { get; }
    public Action<object, Action, bool> PostServerJob { get; }

    private WinUiAbi(Assembly win32, Assembly @base)
    {
        _win32 = win32;
        _base = @base;
        _queryInterface = typeof(MicroComRuntime).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(MicroComRuntime.QueryInterface) && method.IsGenericMethodDefinition);

        IVisualType = RequireWin32Type("Avalonia.Win32.WinRT.IVisual");
        IContainerVisualType = RequireWin32Type("Avalonia.Win32.WinRT.IContainerVisual");
        ICompositionBrushType = RequireWin32Type("Avalonia.Win32.WinRT.ICompositionBrush");
        IGraphicsEffectSourceType = RequireWin32Type("Avalonia.Win32.WinRT.IGraphicsEffectSource");
        IGraphicsEffectType = RequireWin32Type("Avalonia.Win32.WinRT.IGraphicsEffect");
        WinUiSurfaceType = RequireWin32Type("Avalonia.Win32.WinRT.Composition.WinUiCompositedWindowSurface");
        WinUiWindowType = RequireWin32Type("Avalonia.Win32.WinRT.Composition.WinUiCompositedWindow");
        WinUiUtilsType = RequireWin32Type("Avalonia.Win32.WinRT.Composition.WinUiCompositionUtils");
        HStringType = RequireWin32Type("Avalonia.Win32.WinRT.HStringInterop");
        NativeWinRtMethodsType = RequireWin32Type("Avalonia.Win32.WinRT.NativeWinRTMethods");

        var compositionVisual = typeof(CompositionVisual);
        var serverVisual = RequireBaseType("Avalonia.Rendering.Composition.Server.ServerCompositionVisual");
        var simpleServerObject = RequireBaseType("Avalonia.Rendering.Composition.Server.SimpleServerObject");
        var serverCompositor = RequireBaseType("Avalonia.Rendering.Composition.Server.ServerCompositor");
        var serverAnimations = RequireBaseType("Avalonia.Rendering.Composition.Server.ServerCompositorAnimations");

        GetServerVisual = FastReflection.CreateGetter<object?>(compositionVisual.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(property => property.Name == "Server" && property.PropertyType == serverVisual));
        GetOwnTransform = FastReflection.CreateGetter<Matrix?>(RequireProperty(serverVisual, "OwnTransform"));
        GetServerParent = FastReflection.CreateGetter<object?>(RequireProperty(serverVisual, "Parent"));
        GetServerOpacity = FastReflection.CreateGetter<float>(RequireProperty(serverVisual, "Opacity"));
        GetServerVisible = FastReflection.CreateGetter<bool>(RequireProperty(serverVisual, "Visible"));
        GetServerCompositor = FastReflection.CreateGetter<object>(RequireProperty(simpleServerObject, "Compositor"));
        GetServerAnimations = FastReflection.CreateGetter<object>(RequireProperty(serverCompositor, "Animations"));
        GetNeedNextTick = FastReflection.CreateGetter<bool>(RequireProperty(serverAnimations, "NeedNextTick"));
        PostServerJob = FastReflection.CreateServerJobInvoker(typeof(Compositor).GetMethod("PostServerJob",
            BindingFlags.Instance | BindingFlags.NonPublic, [typeof(Action), typeof(bool)])
            ?? throw new MissingMethodException(typeof(Compositor).FullName, "PostServerJob"));
    }

    public static WinUiAbi? TryCreate()
    {
        try
        {
            var win32 = typeof(global::Avalonia.Win32PlatformOptions).Assembly;
            var @base = typeof(CompositionVisual).Assembly;
            if (win32.GetName().Version?.ToString() != ExpectedVersion || @base.GetName().Version?.ToString() != ExpectedVersion)
                return null;
            return new WinUiAbi(win32, @base);
        }
        catch (Exception exception)
        {
            MaterialDiagnostics.Write($"Avalonia ABI is unavailable: {exception}");
            return null;
        }
    }

    public object QueryInterface(object value, Type interfaceType)
    {
        if (value is not IUnknown unknown)
            throw new InvalidOperationException($"{value.GetType()} is not a MicroCom proxy.");
        return _queryInterface.MakeGenericMethod(interfaceType).Invoke(null, [unknown])
               ?? throw new InvalidOperationException($"QueryInterface({interfaceType.Name}) returned null.");
    }

    public Type RequireWin32Type(string name) => _win32.GetType(name, true)!;
    public Type RequireBaseType(string name) => _base.GetType(name, true)!;

    public static PropertyInfo RequireProperty(Type type, string name) =>
        type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingMemberException(type.FullName, name);

    public static FieldInfo RequireField(Type type, string name) =>
        type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(type.FullName, name);

    public static MethodInfo RequireMethod(Type type, string name, int parameterCount)
        => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
               .Single(method => method.Name == name && method.GetParameters().Length == parameterCount);
}
