using Microsoft.UI.Text;

namespace OutMapper;

public sealed class ProjectDatasetContent : Border
{
    private readonly TextBlock _nameLabel;

    public ProjectDatasetContent()
    {
        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _nameLabel = new TextBlock
        {
            FontSize = 20,
            FontWeight = FontWeights.SemiBold
        };

        Child = _nameLabel;
    }

    public void SetDataset(string datasetName)
    {
        _nameLabel.Text = datasetName;
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
