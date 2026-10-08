using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SkiaSharp;

namespace CompositionMaterial.Avalonia.Platform.Windows;

internal sealed class ComHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public ComHandle(nint value) : base(true) => SetHandle(value);
    protected override bool ReleaseHandle()
    {
        Marshal.Release(handle);
        return true;
    }
}

internal static unsafe class Direct2DPath
{
    private static readonly Guid FactoryId = new("06152247-6f50-465a-9245-118bfd3b6007");

    [DllImport("d2d1.dll", ExactSpelling = true)]
    private static extern int D2D1CreateFactory(uint factoryType, in Guid iid, nint options, out nint factory);

    public static ComHandle Create(SKPath path, float scale)
    {
        Marshal.ThrowExceptionForHR(D2D1CreateFactory(1, FactoryId, 0, out var factoryPointer));
        using var factory = new ComHandle(factoryPointer);
        var geometry = CreateEmpty(factoryPointer);
        try
        {
            using var sink = Open(geometry.DangerousGetHandle());
            var sinkPointer = sink.DangerousGetHandle();
            if (path.FillType is SKPathFillType.InverseEvenOdd or SKPathFillType.InverseWinding)
                throw new NotSupportedException("Inverse geometry fills are not supported by the native clip.");
            ((delegate* unmanaged[Stdcall]<nint, int, void>)Method(sinkPointer, 3))(
                sinkPointer, path.FillType == SKPathFillType.EvenOdd ? 0 : 1);
            using var iterator = path.CreateIterator(true);
            var points = new SKPoint[4];
            var figureOpen = false;
            SKPathVerb verb;
            while ((verb = iterator.Next(points)) != SKPathVerb.Done)
            {
                switch (verb)
                {
                    case SKPathVerb.Move:
                        if (figureOpen) EndFigure(sinkPointer, false);
                        ((delegate* unmanaged[Stdcall]<nint, Vector2, int, void>)Method(sinkPointer, 5))(
                            sinkPointer, Point(points[0], scale), 0);
                        figureOpen = true;
                        break;
                    case SKPathVerb.Line:
                        var end = Point(points[1], scale);
                        ((delegate* unmanaged[Stdcall]<nint, Vector2*, uint, void>)Method(sinkPointer, 6))(sinkPointer, &end, 1);
                        break;
                    case SKPathVerb.Cubic:
                        var cubic = new Cubic(Point(points[1], scale), Point(points[2], scale), Point(points[3], scale));
                        ((delegate* unmanaged[Stdcall]<nint, Cubic*, uint, void>)Method(sinkPointer, 7))(sinkPointer, &cubic, 1);
                        break;
                    case SKPathVerb.Quad:
                        AddQuadratic(sinkPointer, points[1], points[2], scale);
                        break;
                    case SKPathVerb.Conic:
                        // Direct2D has no rational quadratic primitive. Preserve Skia's curved fill to subpixel accuracy.
                        var quads = SKPath.ConvertConicToQuads(points[0], points[1], points[2], iterator.ConicWeight(), 5);
                        for (var i = 1; i + 1 < quads.Length; i += 2)
                            AddQuadratic(sinkPointer, quads[i], quads[i + 1], scale);
                        break;
                    case SKPathVerb.Close:
                        if (figureOpen) EndFigure(sinkPointer, true);
                        figureOpen = false;
                        break;
                }
            }
            if (figureOpen) EndFigure(sinkPointer, true);
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, int>)Method(sinkPointer, 9))(sinkPointer));
            return geometry;
        }
        catch
        {
            geometry.Dispose();
            throw;
        }
    }

    public static ComHandle CopyToFactory(nint geometry, nint factory)
    {
        var copy = CreateEmpty(factory);
        try
        {
            using var sink = Open(copy.DangerousGetHandle());
            var sinkPointer = sink.DangerousGetHandle();
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, int, nint, float, nint, int>)
                Method(geometry, 9))(geometry, 0, 0, 0.01f, sinkPointer));
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, int>)Method(sinkPointer, 9))(sinkPointer));
            return copy;
        }
        catch
        {
            copy.Dispose();
            throw;
        }
    }

    public static bool Contains(nint geometry, Vector2 point)
    {
        var contains = 0;
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, Vector2, nint, float, int*, int>)
            Method(geometry, 7))(geometry, point, 0, 0.0001f, &contains));
        return contains != 0;
    }

    private static ComHandle CreateEmpty(nint factory)
    {
        nint geometry = 0;
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(factory, 10))(factory, &geometry));
        return new ComHandle(geometry);
    }

    private static ComHandle Open(nint geometry)
    {
        nint sink = 0;
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(geometry, 17))(geometry, &sink));
        return new ComHandle(sink);
    }

    private static void AddQuadratic(nint sink, SKPoint control, SKPoint end, float scale)
    {
        var quadratic = new Quadratic(Point(control, scale), Point(end, scale));
        ((delegate* unmanaged[Stdcall]<nint, Quadratic*, void>)Method(sink, 12))(sink, &quadratic);
    }

    private static void EndFigure(nint sink, bool closed) =>
        ((delegate* unmanaged[Stdcall]<nint, int, void>)Method(sink, 8))(sink, closed ? 1 : 0);

    private static Vector2 Point(SKPoint point, float scale) => new(point.X * scale, point.Y * scale);
    internal static nint Method(nint instance, int slot) => (*(nint**)instance)[slot];

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Cubic(Vector2 Control1, Vector2 Control2, Vector2 End);
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Quadratic(Vector2 Control, Vector2 End);
}
