using Avalonia;
using Avalonia.Headless;
using Xunit;

namespace CompositionMaterial.Avalonia.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AvaloniaTestCollection : ICollectionFixture<AvaloniaTestFixture>
{
    public const string Name = "Avalonia";
}

public sealed class AvaloniaTestFixture : IDisposable
{
    // All Avalonia objects and render loops must share one Dispatcher for the lifetime of this collection.
    private readonly HeadlessUnitTestSession _session =
        HeadlessUnitTestSession.StartNew(typeof(GeometryTestApplication), AvaloniaTestIsolationLevel.PerAssembly);

    public Task RunAsync(Action action) => _session.Dispatch(action, CancellationToken.None);

    public void Dispose() => _session.Dispose();
}

public class GeometryTestApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<Application>().UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
