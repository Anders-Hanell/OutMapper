using SkiaSharp;
using System.IO;
using Uno.Extensions.Markup;
using Path = System.IO.Path;

namespace OutMapper;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        var contentGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            }
        };

        var navigationPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 12,
            Padding = new Thickness(16)
        };

        var settingsUsageContent = new Border
        {
            Padding = new Thickness(16),
            Child = new TextBlock
            {
                Text = "Settings - Usage view",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 20
            }
        };

        var settingsWorkspaceContent = new SettingsWorkspaceContent();
        var settingsProjectsContent = new SettingsProjectsContent();
        var settingsSelectProjectContent = new SettingsSelectProjectContent();
        var settingsCreateProjectContent = new SettingsCreateProjectContent();
        var settingsMultitaskingContent = new SettingsMultitaskingContent();

        var settingsInnerContentControl = new ContentControl
        {
            Content = settingsUsageContent
        };

        var settingsSidebar = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Padding = new Thickness(8)
        };

        var settingsUsageButton = new Button { Content = "Usage" };
        var settingsWorkspaceButton = new Button { Content = "Workspace" };
        var settingsProjectsButton = new Button { Content = "Current Projects" };
        var settingsSelectProjectButton = new Button { Content = "Select Project" };
        var settingsCreateProjectButton = new Button { Content = "Create Project" };
        var settingsMultitaskingButton = new Button { Content = "Multitasking" };

        settingsUsageButton.Click += (_, _) => settingsInnerContentControl.Content = settingsUsageContent;
        settingsWorkspaceButton.Click += (_, _) => settingsInnerContentControl.Content = settingsWorkspaceContent;
        settingsProjectsButton.Click += (_, _) =>
        {
            settingsProjectsContent.Refresh();
            settingsInnerContentControl.Content = settingsProjectsContent;
        };
        settingsSelectProjectButton.Click += (_, _) =>
        {
            settingsSelectProjectContent.Refresh();
            settingsInnerContentControl.Content = settingsSelectProjectContent;
        };
        settingsCreateProjectButton.Click += (_, _) =>
        {
            settingsCreateProjectContent.Reset();
            settingsInnerContentControl.Content = settingsCreateProjectContent;
        };
        settingsMultitaskingButton.Click += (_, _) => settingsInnerContentControl.Content = settingsMultitaskingContent;

        settingsSidebar.Children.Add(settingsUsageButton);
        settingsSidebar.Children.Add(settingsWorkspaceButton);
        settingsSidebar.Children.Add(settingsProjectsButton);
        settingsSidebar.Children.Add(settingsSelectProjectButton);
        settingsSidebar.Children.Add(settingsCreateProjectButton);
        settingsSidebar.Children.Add(settingsMultitaskingButton);

        var settingsContent = new Border
        {
            Padding = new Thickness(16),
            Child = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                Children =
                {
                    settingsSidebar,
                    settingsInnerContentControl
                }
            }
        };

        Grid.SetColumn(settingsInnerContentControl, 1);

        var projectDatasetsContent = new ProjectDatasetsContent();

        var projectsStatusLabel = new TextBlock
        {
            Text = "Projects view",
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 20,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var generatePdfButton = new Button
        {
            Content = "Generate pdf",
            MinHeight = 44,
            MinWidth = 140,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };
        generatePdfButton.Click += (_, _) => GeneratePdf();

        var projectsContent = new Border
        {
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    projectsStatusLabel,
                    generatePdfButton,
                    projectDatasetsContent
                }
            }
        };

        var contentControl = new ContentControl
        {
            Content = settingsContent
        };

        var settingsButton = new Button { Content = "Settings" };
        var projectsButton = new Button { Content = "Projects" };

        settingsButton.Click += (_, _) => contentControl.Content = settingsContent;
        projectsButton.Click += (_, _) =>
        {
            var selectedProject = ProjectFolderService.GetSelectedProjectName(out var error);
            projectsStatusLabel.Text = error ?? (selectedProject is null
                ? "No project selected."
                : $"Current project: {selectedProject}");
            projectDatasetsContent.Refresh(selectedProject);
            contentControl.Content = projectsContent;
        };

        navigationPanel.Children.Add(settingsButton);
        navigationPanel.Children.Add(projectsButton);

        Grid.SetRow(contentControl, 1);

        contentGrid.Children.Add(navigationPanel);
        contentGrid.Children.Add(contentControl);

        this.Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"));
        this.Content = contentGrid;
    }

    private void GeneratePdf()
    {
        var workspaceFolder = SettingsWorkspaceContent.LoadWorkspaceFolderPath();
        if (string.IsNullOrWhiteSpace(workspaceFolder))
        {
            return;
        }

        var outputFile = Path.Combine(workspaceFolder, "Graph.pdf");

        using var stream = File.OpenWrite(outputFile);
        using var document = SKDocument.CreatePdf(stream);
        if (document is null)
        {
            return;
        }

        const float pageWidth = 612f;
        const float pageHeight = 792f;
        const float margin = 72f;
        const float graphSize = 360f;
        const float axisLeft = margin + 40f;
        const float axisBottom = pageHeight - margin - 24f;
        const float axisTop = axisBottom - graphSize;
        const float axisRight = axisLeft + graphSize;
        const float cellSize = graphSize / 3f;

        using var canvas = document.BeginPage(pageWidth, pageHeight);
        canvas.Clear(SKColors.White);

        using var axisPaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 2,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        using var gridLinePaint = new SKPaint
        {
            Color = SKColors.Gray,
            StrokeWidth = 1,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        using var labelPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        using var font = new SKFont
        {
            Size = 18
        };

        // Axes
        canvas.DrawLine(axisLeft, axisBottom, axisRight, axisBottom, axisPaint);
        canvas.DrawLine(axisLeft, axisBottom, axisLeft, axisTop, axisPaint);

        // Colored 3x3 grid
        var gridColors = new[]
        {
            SKColors.SkyBlue,
            SKColors.MediumSeaGreen,
            SKColors.PeachPuff,
            SKColors.LightSteelBlue,
            SKColors.LemonChiffon,
            SKColors.Plum,
            SKColors.LightCoral,
            SKColors.PaleGreen,
            SKColors.Khaki
        };

        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                var squareIndex = row * 3 + col;
                fillPaint.Color = gridColors[squareIndex];
                var left = axisLeft + col * cellSize;
                var top = axisTop + row * cellSize;
                var rect = new SKRect(left, top, left + cellSize, top + cellSize);
                canvas.DrawRect(rect, fillPaint);
                canvas.DrawRect(rect, gridLinePaint);
            }
        }

        // Grid lines
        for (var line = 1; line < 3; line++)
        {
            var x = axisLeft + line * cellSize;
            canvas.DrawLine(x, axisTop, x, axisBottom, gridLinePaint);
            var y = axisTop + line * cellSize;
            canvas.DrawLine(axisLeft, y, axisRight, y, gridLinePaint);
        }

        // Axis titles
        canvas.DrawText("ICP", (axisLeft + axisRight) / 2f, axisBottom + 36f, SKTextAlign.Center, font, labelPaint);

        canvas.Save();
        canvas.Translate(axisLeft - 40f, (axisTop + axisBottom) / 2f);
        canvas.RotateDegrees(-90);
        canvas.DrawText("PRx", 0, 0, SKTextAlign.Center, font, labelPaint);
        canvas.Restore();

        // Graph title
        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        using var titleFont = new SKFont { Size = 24 };
        canvas.DrawText("Sample Graph", pageWidth / 2f, margin, SKTextAlign.Center, titleFont, titlePaint);

        document.EndPage();
        document.Close();
    }
}
