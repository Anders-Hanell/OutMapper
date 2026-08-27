using Uno.Extensions.Markup;

namespace OutMapper;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"));
        this.Content = new MainPanel();
    }
}
