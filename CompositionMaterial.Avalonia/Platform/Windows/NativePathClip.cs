using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Media;
using MicroCom.Runtime;
using SkiaSharp;

namespace CompositionMaterial.Avalonia.Platform.Windows;

[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The pinned geometry implementation is preserved by the linker descriptor.")]
internal static unsafe class NativePathClip
{
    private static readonly StrategyBasedComWrappers Wrappers = new();
    private static readonly Guid PathFactoryId = new("9c1e8c6a-0f33-4751-9437-eb3fb9d3ab07");

    [DllImport("combase.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int WindowsCreateString(string source, uint length, out nint value);
    [DllImport("combase.dll", ExactSpelling = true)]
    private static extern int WindowsDeleteString(nint value);
    [DllImport("combase.dll", ExactSpelling = true)]
    private static extern int RoGetActivationFactory(nint name, in Guid iid, out nint factory);

    internal static SKPath GetFillPath(Geometry geometry)
    {
        var platform = typeof(Geometry).GetProperty("PlatformImpl", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(geometry) ?? throw new NotSupportedException("The geometry renderer is unavailable.");
        var path = platform.GetType().GetProperty("FillPath")?.GetValue(platform) as SKPath
                   ?? throw new NotSupportedException("The native geometry clip requires the pinned Skia renderer.");
        return new SKPath(path);
    }

    public static object Create(WinUiAbi abi, object compositor, Geometry geometry, float scale)
    {
        using var path = GetFillPath(geometry);
        var source = new NativeGeometrySource(Direct2DPath.Create(path, scale));
        var unknown = Wrappers.GetOrCreateComInterfaceForObject(source, CreateComInterfaceFlags.None);
        nint sourcePointer = 0;
        nint compositionPath = 0;
        nint name = 0;
        nint factory = 0;
        object? compositor5 = null;
        object? compositor6 = null;
        try
        {
            var sourceId = NativeGeometrySource.SourceId;
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(unknown, ref sourceId, out sourcePointer));
            const string runtimeClass = "Windows.UI.Composition.CompositionPath";
            Marshal.ThrowExceptionForHR(WindowsCreateString(runtimeClass, (uint)runtimeClass.Length, out name));
            Marshal.ThrowExceptionForHR(RoGetActivationFactory(name, PathFactoryId, out factory));
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, nint*, int>)
                Direct2DPath.Method(factory, 6))(factory, sourcePointer, &compositionPath));
            compositor5 = abi.QueryInterface(compositor, abi.RequireWin32Type("Avalonia.Win32.WinRT.ICompositor5"));
            var geometryPointer = WinUiAbi.RequireMethod(abi.RequireWin32Type("Avalonia.Win32.WinRT.ICompositor5"),
                    "CreatePathGeometryWithPath", 1)
                .Invoke(compositor5, [compositionPath])!;
            var pointer = geometryPointer is nint value ? value : (nint)Pointer.Unbox(geometryPointer);
            using var rawGeometry = MicroComRuntime.CreateProxyFor<IUnknown>(pointer, true);
            var nativeGeometry = abi.QueryInterface(rawGeometry, abi.RequireWin32Type("Avalonia.Win32.WinRT.ICompositionGeometry"));
            try
            {
                compositor6 = abi.QueryInterface(compositor, abi.RequireWin32Type("Avalonia.Win32.WinRT.ICompositor6"));
                return WinUiAbi.RequireMethod(abi.RequireWin32Type("Avalonia.Win32.WinRT.ICompositor6"),
                    "CreateGeometricClipWithGeometry", 1).Invoke(compositor6, [nativeGeometry])!;
            }
            finally
            {
                NativeWindowContext.DisposeProxy(nativeGeometry);
            }
        }
        finally
        {
            NativeWindowContext.DisposeProxy(compositor5);
            NativeWindowContext.DisposeProxy(compositor6);
            if (compositionPath != 0) Marshal.Release(compositionPath);
            if (factory != 0) Marshal.Release(factory);
            if (sourcePointer != 0) Marshal.Release(sourcePointer);
            Marshal.Release(unknown);
            if (name != 0) WindowsDeleteString(name);
            GC.KeepAlive(source);
        }
    }
}
