using System.Collections.Immutable;

namespace DataStructures.Tests;

public class FigureLayoutDataTests
{
    private static readonly OMColor Black = new(0, 0, 0);

    private static OMRect DefaultRect(double left, double top, double width, double height, OMColor? color = null) =>
        new(
            new OMPoint(left, top), new OMPoint(left + width, top),
            new OMPoint(left, top + height), new OMPoint(left + width, top + height),
            width, height, color ?? Black);

    private static HeatmapLayoutData DefaultHeatmapLayout(double left, double top) =>
        HeatmapLayoutData.Create(
            xAxis: new OMLine(new OMPoint(left, top + 40), new OMPoint(left + 40, top + 40), Black, 2),
            yAxis: new OMLine(new OMPoint(left, top + 40), new OMPoint(left, top), Black, 2),
            cellRects: ImmutableArray.Create(DefaultRect(left, top, 40, 40)),
            tickLabels: ImmutableArray<OMTextBox>.Empty,
            axisTitles: ImmutableArray<OMTextBox>.Empty)
            .Should().BeOfType<Success<HeatmapLayoutData>>().Subject.Value;

    private static Result<FigureLayoutData> CreateValid(
        double pageWidth = 612, double pageHeight = 792,
        ImmutableArray<HeatmapLayoutData>? heatmapLayouts = null,
        ImmutableArray<OMTextBox>? labels = null)
    {
        return FigureLayoutData.Create(
            pageWidth: pageWidth, pageHeight: pageHeight,
            heatmapLayouts: heatmapLayouts ?? ImmutableArray.Create(
                DefaultHeatmapLayout(50, 0), DefaultHeatmapLayout(100, 0)),
            // One label per cell in the grid, including the empty one (at left=0) - Labels covers
            // graph and empty cells alike.
            labels: labels ?? ImmutableArray.Create(
                new OMTextBox("Z", DefaultRect(0, 0, 50, 16), OMTextRotation.Horizontal),
                new OMTextBox("A", DefaultRect(50, 0, 40, 16), OMTextRotation.Horizontal),
                new OMTextBox("B", DefaultRect(100, 0, 40, 16), OMTextRotation.Horizontal)));
    }

    [Fact]
    public void Create_succeeds_and_exposes_the_page_size()
    {
        var layout = CreateValid(pageWidth: 612, pageHeight: 792)
            .Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        layout.PageWidth.Should().Be(612);
        layout.PageHeight.Should().Be(792);
    }

    [Fact]
    public void Create_succeeds_and_exposes_each_graphs_full_heatmap_layout()
    {
        var layout = CreateValid().Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        layout.HeatmapLayouts.Should().HaveCount(2);
        layout.HeatmapLayouts[0].CellRects.Single().TopLeft.Should().Be(new OMPoint(50, 0));
        layout.HeatmapLayouts[1].CellRects.Single().TopLeft.Should().Be(new OMPoint(100, 0));
    }

    [Fact]
    public void Create_succeeds_and_exposes_a_label_for_every_cell_including_empty_ones()
    {
        var layout = CreateValid().Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        layout.Labels.Should().HaveCount(3);
        layout.Labels.Select(box => box.Text).Should().Equal("Z", "A", "B");
        layout.Labels.Should().OnlyContain(box => box.Rotation == OMTextRotation.Horizontal);
    }

    [Fact]
    public void Create_succeeds_with_no_labels_when_the_figure_has_no_label_style()
    {
        var layout = CreateValid(labels: ImmutableArray<OMTextBox>.Empty)
            .Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        layout.Labels.Should().BeEmpty();
    }

    [Fact]
    public void Create_fails_when_the_page_width_is_not_positive()
    {
        var result = CreateValid(pageWidth: 0);

        result.Should().BeOfType<Failure<FigureLayoutData>>();
    }

    [Fact]
    public void Create_fails_when_the_page_height_is_not_positive()
    {
        var result = CreateValid(pageHeight: 0);

        result.Should().BeOfType<Failure<FigureLayoutData>>();
    }

    [Fact]
    public void Create_fails_when_a_labels_rect_width_is_not_positive()
    {
        var result = CreateValid(
            labels: ImmutableArray.Create(new OMTextBox("A", DefaultRect(50, 0, 0, 16), OMTextRotation.Horizontal)));

        result.Should().BeOfType<Failure<FigureLayoutData>>();
    }
}
