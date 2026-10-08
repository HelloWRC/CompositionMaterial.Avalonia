using System.Numerics;
using System.Reflection;
using CompositionMaterial.Avalonia.Materials;
using MaterialDefinition = CompositionMaterial.Avalonia.Materials.CompositionMaterial;
using CompositionMaterial.Avalonia.Internal;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;

namespace CompositionMaterial.Avalonia.Platform.Windows;

[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Clip methods are preserved by the package descriptor.")]
internal sealed class NativeMaterialVisual : IDisposable
{
    private readonly NativeWindowContext _owner;
    private readonly WinUiAbi _abi;
    private readonly object _sprite;
    private readonly object _visual;
    private readonly Action<object, Matrix4x4> _setTransform;
    private readonly Action<object, Vector2> _setSize;
    private readonly Action<object, float> _setOpacity;
    private readonly Action<object, int> _setVisible;
    private object? _brush;
    private object? _frostBrush;
    private object? _childContainer;
    private object? _childChildren;
    private object? _frostSprite;
    private object? _frostVisual;
    private object? _clipGeometry;
    private object? _pathClip;
    private Matrix4x4 _lastTransform;
    private Vector2 _lastSize;
    private float _lastOpacity = -1;
    private int _lastVisible = -1;
    private bool _disposed;

    internal NativeMaterialVisual(NativeWindowContext owner, object sprite, object visual)
    {
        _owner = owner;
        _abi = owner.Abi;
        _sprite = sprite;
        _visual = visual;
        var visualType = _abi.IVisualType;
        _setTransform = FastReflection.CreateSetter<Matrix4x4>(WinUiAbi.RequireMethod(visualType, "SetTransformMatrix", 1));
        _setSize = FastReflection.CreateSetter<Vector2>(WinUiAbi.RequireMethod(visualType, "SetSize", 1));
        _setOpacity = FastReflection.CreateSetter<float>(WinUiAbi.RequireMethod(visualType, "SetOpacity", 1));
        _setVisible = FastReflection.CreateSetter<int>(WinUiAbi.RequireMethod(visualType, "SetIsVisible", 1));
    }

    public bool SetMaterial(MaterialDefinition? material)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_owner.SyncRoot)
        {
            NativeMaterialDefinition? definition = null;
            try
            {
                definition = NativeMaterialCompiler.Compile(_owner, material);
                if (definition is null)
                    return false;
                var spriteType = _abi.RequireWin32Type("Avalonia.Win32.WinRT.ISpriteVisual");
                WinUiAbi.RequireMethod(spriteType, "SetBrush", 1).Invoke(_sprite, [definition.PrimaryBrush]);
                ConfigureFrostLayer(definition.FrostBrush, definition.FrostOpacity);
                NativeWindowContext.DisposeProxy(_brush);
                _brush = definition.PrimaryBrush;
                return true;
            }
            catch (Exception exception)
            {
                MaterialDiagnostics.Write($"material compilation failed: {exception}");
                definition?.Dispose();
                return false;
            }
        }
    }

    public void UpdateGeometry(Vector2 size, float radius, Geometry? clip = null, float scale = 1)
    {
        if (_disposed)
            return;
        lock (_owner.SyncRoot)
        {
            if (_lastSize != size)
            {
                _setSize(_visual, size);
                if (_frostVisual is not null)
                    _setSize(_frostVisual, size);
                _lastSize = size;
            }

            if (clip is not null)
            {
                var pathClip = NativePathClip.Create(_abi, _owner.Compositor, clip, scale);
                object? clipInterface = null;
                try
                {
                    clipInterface = _abi.QueryInterface(pathClip, _abi.RequireWin32Type("Avalonia.Win32.WinRT.ICompositionClip"));
                    WinUiAbi.RequireMethod(_abi.IVisualType, "SetClip", 1).Invoke(_visual, [clipInterface]);
                    NativeWindowContext.DisposeProxy(_pathClip);
                    _pathClip = pathClip;
                    pathClip = null!;
                }
                finally
                {
                    NativeWindowContext.DisposeProxy(clipInterface);
                    NativeWindowContext.DisposeProxy(pathClip);
                }
                return;
            }

            if (_pathClip is not null)
            {
                NativeWindowContext.DisposeProxy(_pathClip);
                _pathClip = null;
                NativeWindowContext.DisposeProxy(_clipGeometry);
                _clipGeometry = null;
            }

            if (_clipGeometry is null)
            {
                var visualArray = Array.CreateInstance(_abi.IVisualType, 1);
                visualArray.SetValue(_visual, 0);
                _clipGeometry = _abi.WinUiUtilsType.GetMethod("ClipVisual", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
                    .Invoke(null, [_owner.Compositor, (float?)radius, visualArray]);
            }

            if (_clipGeometry is not null)
            {
                var geometryType = _abi.RequireWin32Type("Avalonia.Win32.WinRT.ICompositionRoundedRectangleGeometry");
                WinUiAbi.RequireMethod(geometryType, "SetSize", 1).Invoke(_clipGeometry, [size]);
                WinUiAbi.RequireMethod(geometryType, "SetCornerRadius", 1).Invoke(_clipGeometry, [new Vector2(radius)]);
            }
        }
    }

    public void UpdateMaterialParameters(MaterialDefinition? material)
    {
        if (_disposed)
            return;
        while (material is RevealMaterial { BaseMaterial: { } baseMaterial })
            material = baseMaterial;
        if (material is not LiquidGlassMaterial liquid || _frostVisual is null)
            return;
        lock (_owner.SyncRoot)
            _setOpacity(_frostVisual, (float)Math.Clamp(liquid.FrostedOpacity, 0, 1));
    }

    public bool UpdateFrame(Matrix4x4 transform, float opacity, bool visible)
    {
        if (_disposed)
            return false;
        var changed = false;
        lock (_owner.SyncRoot)
        {
            if (!NearlyEqual(_lastTransform, transform))
            {
                _setTransform(_visual, transform);
                _lastTransform = transform;
                changed = true;
            }
            if (Math.Abs(_lastOpacity - opacity) > 0.0001f)
            {
                _setOpacity(_visual, opacity);
                _lastOpacity = opacity;
                changed = true;
            }
            var intVisible = visible ? 1 : 0;
            if (_lastVisible != intVisible)
            {
                _setVisible(_visual, intVisible);
                _lastVisible = intVisible;
                changed = true;
            }
        }
        return changed;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _owner.Remove(this, _visual);
        DisposeFromOwner();
    }

    internal void DisposeFromOwner()
    {
        if (_disposed)
            return;
        _disposed = true;
        RemoveFrostLayer();
        NativeWindowContext.DisposeProxy(_pathClip);
        NativeWindowContext.DisposeProxy(_clipGeometry);
        NativeWindowContext.DisposeProxy(_brush);
        NativeWindowContext.DisposeProxy(_visual);
        NativeWindowContext.DisposeProxy(_sprite);
    }

    private void ConfigureFrostLayer(object? brush, float opacity)
    {
        if (brush is null || opacity <= 0)
        {
            RemoveFrostLayer();
            return;
        }

        EnsureFrostLayer();
        var spriteType = _abi.RequireWin32Type("Avalonia.Win32.WinRT.ISpriteVisual");
        WinUiAbi.RequireMethod(spriteType, "SetBrush", 1).Invoke(_frostSprite, [brush]);
        _setOpacity(_frostVisual!, Math.Clamp(opacity, 0, 1));
        NativeWindowContext.DisposeProxy(_frostBrush);
        _frostBrush = brush;
    }

    private void EnsureFrostLayer()
    {
        if (_frostVisual is not null)
            return;

        _childContainer = _abi.QueryInterface(_sprite, _abi.IContainerVisualType);
        _childChildren = WinUiAbi.RequireProperty(_abi.IContainerVisualType, "Children").GetValue(_childContainer)
                         ?? throw new InvalidOperationException("Unable to access the material visual children.");
        var compositorType = _abi.RequireWin32Type("Avalonia.Win32.WinRT.ICompositor");
        _frostSprite = WinUiAbi.RequireMethod(compositorType, "CreateSpriteVisual", 0).Invoke(_owner.Compositor, null)
                       ?? throw new InvalidOperationException("Unable to create the frosted glass layer.");
        _frostVisual = _abi.QueryInterface(_frostSprite, _abi.IVisualType);
        var collectionType = _abi.RequireWin32Type("Avalonia.Win32.WinRT.IVisualCollection");
        WinUiAbi.RequireMethod(collectionType, "InsertAtTop", 1).Invoke(_childChildren, [_frostVisual]);
        _setSize(_frostVisual, _lastSize);
    }

    private void RemoveFrostLayer()
    {
        if (_childChildren is not null && _frostVisual is not null)
        {
            try
            {
                var collectionType = _abi.RequireWin32Type("Avalonia.Win32.WinRT.IVisualCollection");
                WinUiAbi.RequireMethod(collectionType, "Remove", 1).Invoke(_childChildren, [_frostVisual]);
            }
            catch
            {
                // The native visual tree may already be disposed.
            }
        }

        NativeWindowContext.DisposeProxy(_frostBrush);
        NativeWindowContext.DisposeProxy(_frostVisual);
        NativeWindowContext.DisposeProxy(_frostSprite);
        NativeWindowContext.DisposeProxy(_childChildren);
        NativeWindowContext.DisposeProxy(_childContainer);
        _frostBrush = null;
        _frostVisual = null;
        _frostSprite = null;
        _childChildren = null;
        _childContainer = null;
    }

    private static bool NearlyEqual(in Matrix4x4 left, in Matrix4x4 right)
    {
        const float epsilon = 0.0001f;
        return Math.Abs(left.M11 - right.M11) < epsilon && Math.Abs(left.M12 - right.M12) < epsilon &&
               Math.Abs(left.M21 - right.M21) < epsilon && Math.Abs(left.M22 - right.M22) < epsilon &&
               Math.Abs(left.M41 - right.M41) < epsilon && Math.Abs(left.M42 - right.M42) < epsilon;
    }
}
