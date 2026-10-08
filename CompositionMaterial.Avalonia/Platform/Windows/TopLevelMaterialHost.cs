using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.Layout;
using CompositionMaterial.Avalonia.Internal;

namespace CompositionMaterial.Avalonia.Platform.Windows;

internal sealed class TopLevelMaterialHost : IDisposable
{
    private static readonly ConditionalWeakTable<TopLevel, TopLevelMaterialHost> Hosts = new();
    private static readonly WinUiAbi? Abi = WinUiAbi.TryCreate();

    private readonly TopLevel _topLevel;
    private readonly NativeWindowContext _native;
    private readonly Compositor _compositor;
    private readonly List<Attachment> _attachments = [];
    private readonly Action _afterCommit;
    private readonly MethodInfo _removeAfterCommit;
    private bool _framePending;
    private bool _disposed;
    private volatile bool _serverNeedsFrames;
    private int _stableFrames;
    private double _renderScaling = 1;

    private TopLevelMaterialHost(TopLevel topLevel, NativeWindowContext native, Compositor compositor)
    {
        _topLevel = topLevel;
        _native = native;
        _compositor = compositor;
        _afterCommit = OnAfterCommit;
        var afterCommit = typeof(Compositor).GetEvent("AfterCommit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                          ?? throw new MissingMemberException(typeof(Compositor).FullName, "AfterCommit");
        afterCommit.GetAddMethod(true)!.Invoke(_compositor, [_afterCommit]);
        _removeAfterCommit = afterCommit.GetRemoveMethod(true)!;
        _topLevel.Closed += TopLevelClosed;
        _topLevel.ScalingChanged += ScalingChanged;
        _topLevel.ActualThemeVariantChanged += ActualThemeVariantChanged;
    }

    public static IMaterialControlAttachment? TryAttach(CompositionMaterialControl control)
    {
        if (Abi is null)
        {
            control.SetPlatformMode(MaterialRenderingMode.Fallback);
            return null;
        }

        var topLevel = TopLevel.GetTopLevel(control);
        var elementVisual = ElementComposition.GetElementVisual(control);
        if (topLevel is null || elementVisual is null)
        {
            control.SetPlatformMode(MaterialRenderingMode.Fallback);
            return null;
        }

        try
        {
            var host = Hosts.GetValue(topLevel, key =>
            {
                var native = NativeWindowContext.TryCreate(key, Abi)
                             ?? throw new InvalidOperationException("The active renderer is not WinUIComposition.");
                return new TopLevelMaterialHost(key, native, elementVisual.Compositor);
            });
            if (host._disposed)
                return null;
            return host.Attach(control, elementVisual);
        }
        catch (Exception exception)
        {
            MaterialDiagnostics.Write($"native attachment failed: {exception}");
            control.SetPlatformMode(MaterialRenderingMode.Fallback);
            return null;
        }
    }

    private Attachment Attach(CompositionMaterialControl control, CompositionVisual compositionVisual)
    {
        var serverVisual = _native.Abi.GetServerVisual(compositionVisual)
                           ?? throw new InvalidOperationException("Avalonia server visual is unavailable.");
        var attachment = new Attachment(this, control, compositionVisual, serverVisual, _native.CreateMaterialVisual());
        lock (_attachments)
            _attachments.Add(attachment);
        attachment.RefreshMaterial();
        attachment.RefreshGeometry();
        Wake();
        return attachment;
    }

    private void Detach(Attachment attachment)
    {
        lock (_attachments)
            _attachments.Remove(attachment);
        attachment.Native.Dispose();
        attachment.Control.SetPlatformMode(MaterialRenderingMode.Fallback);
    }

    private void OnAfterCommit()
    {
        if (_disposed || _framePending)
            return;
        if (Dispatcher.UIThread.CheckAccess())
            Wake();
        else
            Dispatcher.UIThread.Post(Wake, DispatcherPriority.Render);
    }

    private void Wake()
    {
        if (_disposed || _framePending || AttachmentCount == 0)
            return;
        _stableFrames = 0;
        _framePending = true;
        _topLevel.RequestAnimationFrame(AnimationFrame);
    }

    private void AnimationFrame(TimeSpan _)
    {
        if (_disposed || AttachmentCount == 0)
        {
            _framePending = false;
            return;
        }

        if (!_serverNeedsFrames && Volatile.Read(ref _stableFrames) >= 2)
        {
            _framePending = false;
            return;
        }

        Volatile.Write(ref _renderScaling, _topLevel.RenderScaling);
        _native.Abi.PostServerJob(_compositor, SynchronizeOnServer, false);
        _topLevel.RequestAnimationFrame(AnimationFrame);
    }

    private void SynchronizeOnServer()
    {
        var anyChanged = false;
        var anyLiveTracking = false;
        object? firstServerVisual = null;
        var scaling = Volatile.Read(ref _renderScaling);

        foreach (var attachment in AttachmentSnapshot())
        {
            if (attachment.IsDisposed || !attachment.IsNativeActive)
                continue;
            firstServerVisual ??= attachment.ServerVisual;
            anyLiveTracking |= attachment.LiveTracking;
            var state = ReadServerState(attachment.ServerVisual, scaling);
            anyChanged |= attachment.Native.UpdateFrame(state.Transform, state.Opacity, state.Visible);
        }

        var needsTick = false;
        if (anyLiveTracking && firstServerVisual is not null)
        {
            try
            {
                var serverCompositor = _native.Abi.GetServerCompositor(firstServerVisual);
                var animations = _native.Abi.GetServerAnimations(serverCompositor);
                needsTick = _native.Abi.GetNeedNextTick(animations);
            }
            catch
            {
                needsTick = false;
            }
        }

        _serverNeedsFrames = needsTick;
        if (anyChanged || needsTick)
            Volatile.Write(ref _stableFrames, 0);
        else
            Interlocked.Increment(ref _stableFrames);
    }

    private ServerVisualState ReadServerState(object serverVisual, double scaling)
    {
        var matrix = Matrix.Identity;
        var opacity = 1f;
        var visible = true;
        object? current = serverVisual;
        var depth = 0;
        while (current is not null && depth++ < 512)
        {
            var own = _native.Abi.GetOwnTransform(current);
            if (own.HasValue)
                matrix *= own.Value;
            opacity *= _native.Abi.GetServerOpacity(current);
            visible &= _native.Abi.GetServerVisible(current);
            current = _native.Abi.GetServerParent(current);
        }

        return new ServerVisualState(NativeTransformMath.ToPixels(matrix, scaling), Math.Clamp(opacity, 0, 1), visible);
    }

    private void ScalingChanged(object? sender, EventArgs e)
    {
        foreach (var attachment in AttachmentSnapshot())
            attachment.RefreshGeometry();
        Wake();
    }

    private void TopLevelClosed(object? sender, EventArgs e) => Dispose();

    private void ActualThemeVariantChanged(object? sender, EventArgs e)
    {
        foreach (var attachment in AttachmentSnapshot())
            attachment.RefreshMaterial();
        Wake();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _topLevel.Closed -= TopLevelClosed;
        _topLevel.ScalingChanged -= ScalingChanged;
        _topLevel.ActualThemeVariantChanged -= ActualThemeVariantChanged;
        try
        {
            _removeAfterCommit.Invoke(_compositor, [_afterCommit]);
        }
        catch
        {
            // Compositor may already be disposed.
        }
        foreach (var attachment in AttachmentSnapshot())
            attachment.DisposeFromHost();
        lock (_attachments)
            _attachments.Clear();
        _native.Dispose();
    }

    private int AttachmentCount
    {
        get { lock (_attachments) return _attachments.Count; }
    }

    private Attachment[] AttachmentSnapshot()
    {
        lock (_attachments)
            return _attachments.ToArray();
    }

    private readonly record struct ServerVisualState(Matrix4x4 Transform, float Opacity, bool Visible);

    private sealed class Attachment : IMaterialControlAttachment
    {
        private readonly TopLevelMaterialHost _host;
        private bool _disposed;

        public CompositionMaterialControl Control { get; }
        public CompositionVisual CompositionVisual { get; }
        public object ServerVisual { get; }
        public NativeMaterialVisual Native { get; }
        public bool IsDisposed => _disposed;
        public volatile bool IsNativeActive;
        public volatile bool LiveTracking;

        public Attachment(TopLevelMaterialHost host, CompositionMaterialControl control,
            CompositionVisual compositionVisual, object serverVisual, NativeMaterialVisual native)
        {
            _host = host;
            Control = control;
            CompositionVisual = compositionVisual;
            ServerVisual = serverVisual;
            Native = native;
            LiveTracking = control.EnableLiveTransformTracking;
            Control.EffectiveViewportChanged += EffectiveViewportChanged;
        }

        public void MaterialChanged()
        {
            if (_disposed)
                return;
            RefreshMaterial();
            _host.Wake();
        }

        public void GeometryChanged()
        {
            if (_disposed)
                return;
            RefreshGeometry();
            _host.Wake();
        }

        public void MaterialParametersChanged()
        {
            if (_disposed)
                return;
            Native.UpdateMaterialParameters(Control.Material);
            _host.Wake();
        }

        public void VisualStateChanged()
        {
            if (!_disposed)
            {
                LiveTracking = Control.EnableLiveTransformTracking;
                _host.Wake();
            }
        }

        public void RefreshMaterial()
        {
            IsNativeActive = Native.SetMaterial(Control.Material);
            Control.SetPlatformMode(IsNativeActive ? MaterialRenderingMode.WinUIComposition : MaterialRenderingMode.Fallback);
            if (!IsNativeActive)
                Native.UpdateFrame(Matrix4x4.Identity, 0, false);
        }

        public void RefreshGeometry()
        {
            var scaling = _host._topLevel.RenderScaling;
            var size = new Vector2((float)(Control.Bounds.Width * scaling), (float)(Control.Bounds.Height * scaling));
            var corner = Control.CornerRadius;
            var radius = Math.Max(Math.Max(corner.TopLeft, corner.TopRight), Math.Max(corner.BottomRight, corner.BottomLeft));
            try
            {
                if (!IsNativeActive)
                    RefreshMaterial();
                Native.UpdateGeometry(size, (float)(radius * scaling), Control.NativeClipGeometry, (float)scaling);
            }
            catch (Exception exception)
            {
                MaterialDiagnostics.Write($"native geometry clip failed: {exception}");
                IsNativeActive = false;
                Native.UpdateFrame(Matrix4x4.Identity, 0, false);
                Control.SetPlatformMode(MaterialRenderingMode.Fallback);
            }
        }

        private void EffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            RefreshGeometry();
            _host.Wake();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Control.EffectiveViewportChanged -= EffectiveViewportChanged;
            _host.Detach(this);
        }

        public void DisposeFromHost()
        {
            if (_disposed)
                return;
            _disposed = true;
            Control.EffectiveViewportChanged -= EffectiveViewportChanged;
            Native.DisposeFromOwner();
            Control.SetPlatformMode(MaterialRenderingMode.Fallback);
        }
    }
}
