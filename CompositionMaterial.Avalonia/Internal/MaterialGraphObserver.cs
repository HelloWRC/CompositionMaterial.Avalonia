using System.Collections.Specialized;
using Avalonia;
using CompositionMaterial.Avalonia.Materials;
using MaterialDefinition = CompositionMaterial.Avalonia.Materials.CompositionMaterial;

namespace CompositionMaterial.Avalonia.Internal;

internal sealed class MaterialGraphObserver : IDisposable
{
    private readonly Action<AvaloniaObject?, AvaloniaProperty?> _changed;
    private readonly List<AvaloniaObject> _objects = [];
    private readonly List<INotifyCollectionChanged> _collections = [];
    private bool _disposed;

    public MaterialGraphObserver(MaterialDefinition? material, Action<AvaloniaObject?, AvaloniaProperty?> changed)
    {
        _changed = changed;
        Rebuild(material);
    }

    public void Rebuild(MaterialDefinition? material)
    {
        Unsubscribe();
        if (_disposed || material is null)
            return;
        var visited = new HashSet<AvaloniaObject>(ReferenceEqualityComparer.Instance);
        Visit(material, visited);
        foreach (var value in _objects)
            value.PropertyChanged += PropertyChanged;
        foreach (var value in _collections)
            value.CollectionChanged += CollectionChanged;
    }

    private void Visit(AvaloniaObject value, HashSet<AvaloniaObject> visited)
    {
        if (!visited.Add(value))
            return;
        _objects.Add(value);
        switch (value)
        {
            case CustomCompositionMaterial custom when custom.RootBrush is { } root:
                Visit(root, visited);
                break;
            case RevealMaterial reveal when reveal.BaseMaterial is { } baseMaterial:
                Visit(baseMaterial, visited);
                break;
            case UnaryMaterialBrushNode unary when unary.Source is { } source:
                Visit(source, visited);
                break;
            case BlendBrushNode blend:
                if (blend.Background is { } background) Visit(background, visited);
                if (blend.Foreground is { } foreground) Visit(foreground, visited);
                break;
            case CompositeBrushNode composite:
                _collections.Add(composite.Sources);
                foreach (var sourceNode in composite.Sources) Visit(sourceNode, visited);
                break;
            case MaskBrushNode mask:
                if (mask.Source is { } maskSource) Visit(maskSource, visited);
                if (mask.Mask is { } maskNode) Visit(maskNode, visited);
                break;
            case GradientBrushNode gradient:
                _collections.Add(gradient.GradientStops);
                foreach (var stop in gradient.GradientStops) Visit(stop, visited);
                break;
        }
    }

    private void PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        => _changed(sender as AvaloniaObject, e.Property);

    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => _changed(null, null);

    private void Unsubscribe()
    {
        foreach (var value in _objects)
            value.PropertyChanged -= PropertyChanged;
        foreach (var value in _collections)
            value.CollectionChanged -= CollectionChanged;
        _objects.Clear();
        _collections.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        Unsubscribe();
        _disposed = true;
    }
}
