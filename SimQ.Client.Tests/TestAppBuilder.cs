using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(SimQ.Client.Tests.TestAppBuilder))]

namespace SimQ.Client.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
