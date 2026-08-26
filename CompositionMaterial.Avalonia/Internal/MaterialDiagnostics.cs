namespace CompositionMaterial.Avalonia.Internal;

internal static class MaterialDiagnostics
{
    private static readonly bool Enabled =
        string.Equals(Environment.GetEnvironmentVariable("COMPOSITION_MATERIAL_DIAGNOSTICS"), "1", StringComparison.Ordinal);

    public static void Write(string message)
    {
        System.Diagnostics.Debug.WriteLine($"CompositionMaterial: {message}");
        if (Enabled)
            Console.Error.WriteLine($"CompositionMaterial: {message}");
    }
}
