using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
using CompositionMaterial.Avalonia.Internal;
using CompositionMaterial.Avalonia.Materials;
using CompositionMaterial.Avalonia.Platform.Windows;
using MaterialDefinition = CompositionMaterial.Avalonia.Materials.CompositionMaterial;

namespace CompositionMaterial.Avalonia;

/// <summary>
/// Decorates a child with a native WinUI Composition material, with an Avalonia brush fallback.
/// </summary>
public class CompositionMaterialControl : Decorator
{
    public static readonly StyledProperty<MaterialDefinition?> MaterialProperty =
        AvaloniaProperty.Register<CompositionMaterialControl, MaterialDefinition?>(nameof(Material));

    public static readonly StyledProperty<IBrush?> FallbackBrushProperty =
        AvaloniaProperty.Register<CompositionMaterialControl, IBrush?>(nameof(FallbackBrush));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<CompositionMaterialControl>();

    public static readonly StyledProperty<bool> EnableLiveTransformTrackingProperty =
        AvaloniaProperty.Register<CompositionMaterialControl, bool>(nameof(EnableLiveTransformTracking), true);

    private static readonly DirectProperty<CompositionMaterialControl, MaterialRenderingMode> ActualRenderingModePropertyKey =
        AvaloniaProperty.RegisterDirect<CompositionMaterialControl, MaterialRenderingMode>(
            nameof(ActualRenderingMode), control => control._actualRenderingMode);

    private static readonly DirectProperty<CompositionMaterialControl, bool> IsNativeMaterialActivePropertyKey =
        AvaloniaProperty.RegisterDirect<CompositionMaterialControl, bool>(
            nameof(IsNativeMaterialActive), control => control._isNativeMaterialActive);

    public static readonly DirectProperty<CompositionMaterialControl, MaterialRenderingMode> ActualRenderingModeProperty =
        ActualRenderingModePropertyKey;

    public static readonly DirectProperty<CompositionMaterialControl, bool> IsNativeMaterialActiveProperty =
        IsNativeMaterialActivePropertyKey;

    private IMaterialControlAttachment? _attachment;
    private MaterialGraphObserver? _materialObserver;
    private MaterialRenderingMode _actualRenderingMode;
    private bool _isNativeMaterialActive;
    private Point _pointer;
    private bool _pointerOver;
    private RectangleGeometry? _roundedClip;
    private Geometry? _observedClip;
    private int _attachAttempt;
    private bool _attachFramePending;

    static CompositionMaterialControl()
    {
        AffectsRender<CompositionMaterialControl>(MaterialProperty, FallbackBrushProperty, CornerRadiusProperty);
        AffectsMeasure<CompositionMaterialControl>(PaddingProperty);
    }

    public CompositionMaterialControl()
    {
        ClipToBounds = true;
    }

    public MaterialDefinition? Material
    {
        get => GetValue(MaterialProperty);
        set => SetValue(MaterialProperty, value);
    }

    public IBrush? FallbackBrush
    {
        get => GetValue(FallbackBrushProperty);
        set => SetValue(FallbackBrushProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public bool EnableLiveTransformTracking
    {
        get => GetValue(EnableLiveTransformTrackingProperty);
        set => SetValue(EnableLiveTransformTrackingProperty, value);
    }

    public MaterialRenderingMode ActualRenderingMode => _actualRenderingMode;
    public bool IsNativeMaterialActive => _isNativeMaterialActive;

    internal Geometry? NativeClipGeometry => ReferenceEquals(Clip, _roundedClip) ? null : Clip;

    public override void Render(DrawingContext context)
    {
        var rect = new Rect(Bounds.Size);
        if (!_isNativeMaterialActive && FallbackBrush is { } fallback)
            context.DrawRectangle(fallback, null, new RoundedRect(rect, CornerRadius));

        if (_isNativeMaterialActive)
            MaterialOverlayRenderer.Render(context, rect, CornerRadius, Material, _pointer, _pointerOver);

        base.Render(context);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ObserveMaterial(Material);
        UpdateRoundedClip();
        ObserveClip(Clip);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _attachAttempt = 0;
        TryAttach();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        _attachFramePending = false;
        _attachment?.Dispose();
        _attachment = null;
        SetPlatformMode(MaterialRenderingMode.Fallback);
        base.OnUnloaded(e);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attachment?.Dispose();
        _attachment = null;
        _attachFramePending = false;
        ObserveMaterial(null);
        ObserveClip(null);
        SetPlatformMode(MaterialRenderingMode.Fallback);
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MaterialProperty)
        {
            ObserveMaterial(change.GetNewValue<MaterialDefinition?>());
            _attachment?.MaterialChanged();
        }
        else if (change.Property == BoundsProperty || change.Property == CornerRadiusProperty)
        {
            UpdateRoundedClip();
            _attachment?.GeometryChanged();
        }
        else if (change.Property == ClipProperty)
        {
            ObserveClip(Clip);
            _attachment?.GeometryChanged();
        }
        else if (change.Property == ClipToBoundsProperty)
        {
            UpdateRoundedClip();
            _attachment?.GeometryChanged();
        }
        else if (change.Property == IsVisibleProperty || change.Property == OpacityProperty ||
                 change.Property == EnableLiveTransformTrackingProperty || change.Property == ClipToBoundsProperty)
        {
            _attachment?.VisualStateChanged();
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _pointer = e.GetPosition(this);
        if (_pointerOver)
            InvalidateVisual();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _pointerOver = true;
        _pointer = e.GetPosition(this);
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _pointerOver = false;
        InvalidateVisual();
    }

    internal void SetPlatformMode(MaterialRenderingMode mode)
    {
        if (_actualRenderingMode != mode)
        {
            SetAndRaise(ActualRenderingModePropertyKey, ref _actualRenderingMode, mode);
            MaterialDiagnostics.Write($"{GetType().Name} backend changed to {mode}");
        }

        var active = mode == MaterialRenderingMode.WinUIComposition;
        if (_isNativeMaterialActive != active)
        {
            SetAndRaise(IsNativeMaterialActivePropertyKey, ref _isNativeMaterialActive, active);
            InvalidateVisual();
        }
    }

    private void ObserveMaterial(MaterialDefinition? material)
    {
        _materialObserver?.Dispose();
        _materialObserver = material is null ? null : new MaterialGraphObserver(material, MaterialGraphChanged);
    }

    private void MaterialGraphChanged(AvaloniaObject? source, AvaloniaProperty? property)
    {
        _materialObserver?.Rebuild(Material);
        if (RequiresNativeMaterialRebuild(source, property))
            _attachment?.MaterialChanged();
        else
            _attachment?.MaterialParametersChanged();
        InvalidateVisual();
    }

    private static bool RequiresNativeMaterialRebuild(AvaloniaObject? source, AvaloniaProperty? property)
    {
        return source switch
        {
            MicaMaterial when property == MicaMaterial.ThemeModeProperty => true,
            RevealMaterial when property == RevealMaterial.BaseMaterialProperty => true,
            CustomCompositionMaterial => true,
            MaterialBrushNode => true,
            _ => false,
        };
    }

    private void UpdateRoundedClip()
    {
        if (Clip is not null && !ReferenceEquals(Clip, _roundedClip))
            return;
        if (!ClipToBounds)
        {
            if (ReferenceEquals(Clip, _roundedClip))
                Clip = null;
            return;
        }

        var maxRadius = Math.Max(Math.Max(CornerRadius.TopLeft, CornerRadius.TopRight),
            Math.Max(CornerRadius.BottomRight, CornerRadius.BottomLeft));
        _roundedClip ??= new RectangleGeometry();
        _roundedClip.Rect = new Rect(Bounds.Size);
        _roundedClip.RadiusX = maxRadius;
        _roundedClip.RadiusY = maxRadius;
        if (!ReferenceEquals(Clip, _roundedClip))
            Clip = _roundedClip;
    }

    private void ObserveClip(Geometry? clip)
    {
        if (_observedClip is not null)
            _observedClip.Changed -= ClipGeometryChanged;
        _observedClip = clip;
        if (_observedClip is not null)
            _observedClip.Changed += ClipGeometryChanged;
    }

    private void ClipGeometryChanged(object? sender, EventArgs e)
    {
        _attachment?.GeometryChanged();
        InvalidateVisual();
    }

    private void TryAttach()
    {
        if (!IsLoaded || _attachment is not null)
            return;

        _attachment = WindowsMaterialPlatform.TryAttach(this);
        if (_attachment is not null || ++_attachAttempt >= 12)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null || _attachFramePending)
            return;
        _attachFramePending = true;
        topLevel.RequestAnimationFrame(_ =>
        {
            _attachFramePending = false;
            TryAttach();
        });
    }
}
