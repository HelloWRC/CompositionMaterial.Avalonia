using Avalonia.Threading;
using Xunit;

namespace CompositionMaterial.Avalonia.Tests;

[Collection(AvaloniaTestCollection.Name)]
public sealed class AvaloniaTestFixtureTests(AvaloniaTestFixture fixture)
{
    [Fact]
    public async Task Concurrent_callers_share_the_same_Avalonia_dispatcher()
    {
        int? dispatcherThread = null;
        var callers = Enumerable.Range(0, 8).Select(_ => Task.Run(() => fixture.RunAsync(() =>
        {
            Assert.True(Dispatcher.UIThread.CheckAccess());
            dispatcherThread ??= Environment.CurrentManagedThreadId;
            Assert.Equal(dispatcherThread.Value, Environment.CurrentManagedThreadId);
            Assert.False(new CompositionMaterialControl().IsNativeMaterialActive);
        })));

        await Task.WhenAll(callers);
    }
}
