using Avalonia;

namespace CompositionMaterial.Avalonia.Demo;

internal static class Program
{
    public static bool IsSmokeTest { get; private set; }

    [STAThread]
    public static int Main(string[] args)
    {
        IsSmokeTest = args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase);
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new Win32PlatformOptions
        {
            CompositionMode = [Win32CompositionMode.WinUIComposition, Win32CompositionMode.RedirectionSurface],
        })
        .LogToTrace();
}
