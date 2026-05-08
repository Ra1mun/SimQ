using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Themes.Fluent;

namespace SimQ.Client.Tests;

/// <summary>
/// Minimal Avalonia Application for headless testing.
/// Loads FluentTheme + the client styles so Views resolve resources correctly.
/// </summary>
public class TestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());

        // Load the SimQ.Client styles so StaticResource lookups don't fail.
        Styles.Add(new Avalonia.Markup.Xaml.Styling.StyleInclude(
            new Uri("avares://SimQ.Client.Tests"))
        {
            Source = new Uri("avares://SimQ.Client/Styles/Theme.axaml")
        });
        Styles.Add(new Avalonia.Markup.Xaml.Styling.StyleInclude(
            new Uri("avares://SimQ.Client.Tests"))
        {
            Source = new Uri("avares://SimQ.Client/Styles/Controls.axaml")
        });
    }
}
