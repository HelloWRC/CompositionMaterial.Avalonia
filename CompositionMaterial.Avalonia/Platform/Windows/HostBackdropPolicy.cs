using System.Runtime.InteropServices;
using Avalonia.Controls;
using CompositionMaterial.Avalonia.Internal;

namespace CompositionMaterial.Avalonia.Platform.Windows;

internal static partial class HostBackdropPolicy
{
    private const int DwmwaUseHostBackdropBrush = 17;

    public static IDisposable? Acquire(TopLevel topLevel)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            return null;
        var handle = topLevel.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero)
            return null;

        var previous = 0;
        var getResult = DwmGetWindowAttribute(handle, DwmwaUseHostBackdropBrush, ref previous, sizeof(int));
        if (getResult != 0)
            previous = 0;

        var enabled = 1;
        var setResult = DwmSetWindowAttribute(handle, DwmwaUseHostBackdropBrush, ref enabled, sizeof(int));
        MaterialDiagnostics.Write($"DWMWA_USE_HOSTBACKDROPBRUSH=1, HRESULT=0x{setResult:X8}");
        return setResult == 0 ? new Lease(handle, previous) : null;
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmGetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private sealed class Lease(IntPtr handle, int previous) : IDisposable
    {
        private IntPtr _handle = handle;

        public void Dispose()
        {
            var current = Interlocked.Exchange(ref _handle, IntPtr.Zero);
            if (current == IntPtr.Zero)
                return;
            var value = previous;
            var result = DwmSetWindowAttribute(current, DwmwaUseHostBackdropBrush, ref value, sizeof(int));
            MaterialDiagnostics.Write($"DWMWA_USE_HOSTBACKDROPBRUSH restored to {previous}, HRESULT=0x{result:X8}");
        }
    }
}
