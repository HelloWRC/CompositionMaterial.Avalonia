using CompositionMaterial.Avalonia.Materials;

namespace CompositionMaterial.Avalonia.Internal;

internal static class MaterialGraph
{
    public static void Validate(MaterialBrushNode root)
    {
        var visiting = new HashSet<MaterialBrushNode>(ReferenceEqualityComparer.Instance);
        var visited = new HashSet<MaterialBrushNode>(ReferenceEqualityComparer.Instance);
        Visit(root, visiting, visited, _ => { });
    }

    public static bool Contains<T>(MaterialBrushNode root) where T : MaterialBrushNode
    {
        var found = false;
        Visit(root, new HashSet<MaterialBrushNode>(ReferenceEqualityComparer.Instance),
            new HashSet<MaterialBrushNode>(ReferenceEqualityComparer.Instance), node => found |= node is T);
        return found;
    }

    public static BackdropKind? FindBackdrop(MaterialBrushNode root)
    {
        BackdropKind? result = null;
        Visit(root, new HashSet<MaterialBrushNode>(ReferenceEqualityComparer.Instance),
            new HashSet<MaterialBrushNode>(ReferenceEqualityComparer.Instance), node =>
            {
                if (result is null && node is BackdropBrushNode backdrop)
                    result = backdrop.Kind;
            });
        return result;
    }

    private static void Visit(MaterialBrushNode node, HashSet<MaterialBrushNode> visiting,
        HashSet<MaterialBrushNode> visited, Action<MaterialBrushNode> action)
    {
        if (visited.Contains(node))
            return;
        if (!visiting.Add(node))
            throw new InvalidOperationException("The material brush graph contains a cycle.");
        action(node);
        foreach (var child in Children(node))
            Visit(child, visiting, visited, action);
        visiting.Remove(node);
        visited.Add(node);
    }

    private static IEnumerable<MaterialBrushNode> Children(MaterialBrushNode node)
    {
        switch (node)
        {
            case UnaryMaterialBrushNode { Source: { } source }:
                yield return source;
                break;
            case BlendBrushNode blend:
                if (blend.Background is { } background) yield return background;
                if (blend.Foreground is { } foreground) yield return foreground;
                break;
            case CompositeBrushNode composite:
                foreach (var sourceNode in composite.Sources) yield return sourceNode;
                break;
            case MaskBrushNode mask:
                if (mask.Source is { } maskSource) yield return maskSource;
                if (mask.Mask is { } maskNode) yield return maskNode;
                break;
        }
    }
}
