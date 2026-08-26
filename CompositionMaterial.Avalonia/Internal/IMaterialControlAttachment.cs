namespace CompositionMaterial.Avalonia.Internal;

internal interface IMaterialControlAttachment : IDisposable
{
    void MaterialChanged();
    void MaterialParametersChanged();
    void GeometryChanged();
    void VisualStateChanged();
}
