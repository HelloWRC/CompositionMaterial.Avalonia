using System.Numerics;
using System.Reflection;
using Avalonia.Controls;
using CompositionMaterial.Avalonia.Internal;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Styling;

namespace CompositionMaterial.Avalonia.Platform.Windows;

[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The package preserves the pinned Avalonia private ABI.")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The package preserves the pinned Avalonia private ABI.")]
internal sealed class NativeWindowContext : IDisposable
{
    private readonly WinUiAbi _abi;
    private readonly object _rootVisual;
    private readonly object _rootContainer;
    private readonly object _rootChildren;
    private readonly object _hostContainer;
    private readonly object _hostVisual;
    private readonly object _hostChildren;
    private readonly object _swapchainVisual;
    private readonly List<NativeMaterialVisual> _materials = [];
    private bool _disposed;
    private readonly TopLevel _topLevel;
    private readonly IDisposable? _hostBackdropLease;

    public object Compositor { get; }
    public object SyncRoot { get; }
    public WinUiAbi Abi => _abi;
    public bool IsDarkTheme => _topLevel.ActualThemeVariant == ThemeVariant.Dark;

    private NativeWindowContext(TopLevel topLevel, WinUiAbi abi, object compositor, object syncRoot, object rootVisual,
        object rootContainer, object rootChildren, object hostContainer, object hostVisual, object hostChildren,
        object swapchainVisual)
    {
        _topLevel = topLevel;
        _hostBackdropLease = HostBackdropPolicy.Acquire(topLevel);
        _abi = abi;
        Compositor = compositor;
        SyncRoot = syncRoot;
        _rootVisual = rootVisual;
        _rootContainer = rootContainer;
        _rootChildren = rootChildren;
        _hostContainer = hostContainer;
        _hostVisual = hostVisual;
        _hostChildren = hostChildren;
        _swapchainVisual = swapchainVisual;
    }

    public static NativeWindowContext? TryCreate(TopLevel topLevel, WinUiAbi abi)
    {
        try
        {
            var platform = topLevel.PlatformImpl;
            if (platform is null)
                return null;

            var glSurfaceField = FindField(platform.GetType(), "_glSurface");
            var surface = glSurfaceField.GetValue(platform);
            if (surface is null || !abi.WinUiSurfaceType.IsInstanceOfType(surface))
                return null;

            var window = WinUiAbi.RequireField(abi.WinUiSurfaceType, "_window").GetValue(surface)
                         ?? throw new InvalidOperationException("WinUI composited window is missing.");
            var target = WinUiAbi.RequireField(abi.WinUiWindowType, "_target").GetValue(window)
                         ?? throw new InvalidOperationException("WinUI composition target is missing.");
            var shared = WinUiAbi.RequireField(abi.WinUiWindowType, "_shared").GetValue(window)
                         ?? throw new InvalidOperationException("WinUI shared compositor is missing.");
            var swapchainVisual = WinUiAbi.RequireField(abi.WinUiWindowType, "_visual").GetValue(window)
                                  ?? throw new InvalidOperationException("Avalonia swapchain visual is missing.");

            var sharedType = abi.RequireWin32Type("Avalonia.Win32.WinRT.Composition.WinUiCompositionShared");
            var compositor = WinUiAbi.RequireProperty(sharedType, "Compositor").GetValue(shared)
                             ?? throw new InvalidOperationException("WinUI compositor is missing.");
            var syncRoot = WinUiAbi.RequireProperty(sharedType, "SyncRoot").GetValue(shared)
                           ?? throw new InvalidOperationException("WinUI compositor lock is missing.");

            var targetType = abi.RequireWin32Type("Avalonia.Win32.WinRT.ICompositionTarget");
            var rootVisual = WinUiAbi.RequireProperty(targetType, "Root").GetValue(target)
                             ?? throw new InvalidOperationException("WinUI root visual is missing.");
            var rootContainer = abi.QueryInterface(rootVisual, abi.IContainerVisualType);
            var rootChildren = WinUiAbi.RequireProperty(abi.IContainerVisualType, "Children").GetValue(rootContainer)
                               ?? throw new InvalidOperationException("WinUI root children are missing.");

            var compositorType = abi.RequireWin32Type("Avalonia.Win32.WinRT.ICompositor");
            var hostContainer = WinUiAbi.RequireMethod(compositorType, "CreateContainerVisual", 0).Invoke(compositor, null)
                                ?? throw new InvalidOperationException("Unable to create native material container.");
            var hostVisual = abi.QueryInterface(hostContainer, abi.IVisualType);
            var hostChildren = WinUiAbi.RequireProperty(abi.IContainerVisualType, "Children").GetValue(hostContainer)
                               ?? throw new InvalidOperationException("Native material children are missing.");

            lock (syncRoot)
            {
                WinUiAbi.RequireMethod(rootChildren.GetType().GetInterfaces().FirstOrDefault(type => type.Name == "IVisualCollection")
                                       ?? abi.RequireWin32Type("Avalonia.Win32.WinRT.IVisualCollection"), "InsertBelow", 2)
                    .Invoke(rootChildren, [hostVisual, swapchainVisual]);
            }

            MaterialDiagnostics.Write("attached a native container below the Avalonia swapchain visual");
            return new NativeWindowContext(topLevel, abi, compositor, syncRoot, rootVisual, rootContainer, rootChildren,
                hostContainer, hostVisual, hostChildren, swapchainVisual);
        }
        catch (Exception exception)
        {
            MaterialDiagnostics.Write($"WinUI host is unavailable: {exception}");
            return null;
        }
    }

    public NativeMaterialVisual CreateMaterialVisual()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (SyncRoot)
        {
            var compositorType = _abi.RequireWin32Type("Avalonia.Win32.WinRT.ICompositor");
            var sprite = WinUiAbi.RequireMethod(compositorType, "CreateSpriteVisual", 0).Invoke(Compositor, null)
                         ?? throw new InvalidOperationException("Unable to create native material visual.");
            var visual = _abi.QueryInterface(sprite, _abi.IVisualType);
            var collectionType = _abi.RequireWin32Type("Avalonia.Win32.WinRT.IVisualCollection");
            WinUiAbi.RequireMethod(collectionType, "InsertAtTop", 1).Invoke(_hostChildren, [visual]);
            var result = new NativeMaterialVisual(this, sprite, visual);
            _materials.Add(result);
            return result;
        }
    }

    internal void Remove(NativeMaterialVisual material, object visual)
    {
        if (_disposed)
            return;
        lock (SyncRoot)
        {
            _materials.Remove(material);
            try
            {
                var collectionType = _abi.RequireWin32Type("Avalonia.Win32.WinRT.IVisualCollection");
                WinUiAbi.RequireMethod(collectionType, "Remove", 1).Invoke(_hostChildren, [visual]);
            }
            catch
            {
                // The platform visual tree may already be torn down.
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        foreach (var material in _materials.ToArray())
            material.DisposeFromOwner();
        _materials.Clear();

        lock (SyncRoot)
        {
            try
            {
                var collectionType = _abi.RequireWin32Type("Avalonia.Win32.WinRT.IVisualCollection");
                WinUiAbi.RequireMethod(collectionType, "Remove", 1).Invoke(_rootChildren, [_hostVisual]);
            }
            catch
            {
                // The native target can disappear before TopLevel.Closed is raised.
            }
        }

        DisposeProxy(_hostChildren);
        DisposeProxy(_hostVisual);
        DisposeProxy(_hostContainer);
        DisposeProxy(_rootChildren);
        DisposeProxy(_rootContainer);
        DisposeProxy(_rootVisual);
        _hostBackdropLease?.Dispose();
    }

    internal static void DisposeProxy(object? value)
    {
        if (value is IDisposable disposable)
            disposable.Dispose();
    }

    private static FieldInfo FindField(Type type, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var field = current.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field is not null)
                return field;
        }
        throw new MissingFieldException(type.FullName, name);
    }
}
