using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace CompositionMaterial.Avalonia.Platform.Windows;

[GeneratedComInterface]
[Guid("af86e2e0-b12d-4c6a-9c5a-d7aa65101e90")]
internal partial interface INativeInspectable
{
    [PreserveSig] int GetIids(out uint count, out nint iids);
    [PreserveSig] int GetRuntimeClassName(out nint name);
    [PreserveSig] int GetTrustLevel(out int level);
}

[GeneratedComInterface]
[Guid("caff7902-670c-4181-a624-da977203b845")]
internal partial interface INativeGeometrySource2D : INativeInspectable;

[GeneratedComInterface]
[Guid("0657af73-53fd-47cf-84ff-c8492d2a80a3")]
internal partial interface INativeGeometrySourceInterop
{
    [PreserveSig] int GetGeometry(out nint geometry);
    [PreserveSig] int TryGetGeometryUsingFactory(nint factory, out nint geometry);
}

[GeneratedComClass]
internal partial class NativeGeometrySource(ComHandle geometry) : INativeGeometrySource2D, INativeGeometrySourceInterop
{
    public static readonly Guid SourceId = typeof(INativeGeometrySource2D).GUID;
    private readonly ComHandle _geometry = geometry;

    public int GetIids(out uint count, out nint iids)
    {
        count = 1;
        iids = Marshal.AllocCoTaskMem(Marshal.SizeOf<Guid>());
        Marshal.StructureToPtr(SourceId, iids, false);
        return 0;
    }

    public int GetRuntimeClassName(out nint name)
    {
        name = 0;
        return 0;
    }

    public int GetTrustLevel(out int level)
    {
        level = 0;
        return 0;
    }

    public int GetGeometry(out nint geometry)
    {
        geometry = _geometry.DangerousGetHandle();
        Marshal.AddRef(geometry);
        return 0;
    }

    public int TryGetGeometryUsingFactory(nint factory, out nint geometry)
    {
        geometry = 0;
        try
        {
            using var copy = Direct2DPath.CopyToFactory(_geometry.DangerousGetHandle(), factory);
            geometry = copy.DangerousGetHandle();
            Marshal.AddRef(geometry);
            return 0;
        }
        catch (Exception exception)
        {
            return Marshal.GetHRForException(exception);
        }
    }
}
