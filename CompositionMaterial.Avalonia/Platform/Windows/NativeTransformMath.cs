using System.Numerics;
using Avalonia;

namespace CompositionMaterial.Avalonia.Platform.Windows;

internal static class NativeTransformMath
{
    public static Matrix Compose(IEnumerable<Matrix?> transforms)
    {
        var result = Matrix.Identity;
        foreach (var transform in transforms)
        {
            if (transform.HasValue)
                result *= transform.Value;
        }
        return result;
    }

    public static Matrix4x4 ToPixels(Matrix matrix, double scaling)
    {
        var scale = (float)scaling;
        return new Matrix4x4(
            (float)matrix.M11, (float)matrix.M12, 0, (float)matrix.M13,
            (float)matrix.M21, (float)matrix.M22, 0, (float)matrix.M23,
            0, 0, 1, 0,
            (float)matrix.M31 * scale, (float)matrix.M32 * scale, 0, (float)matrix.M33);
    }
}
