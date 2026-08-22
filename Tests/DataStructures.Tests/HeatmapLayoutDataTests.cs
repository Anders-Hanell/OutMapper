using System.Collections.Immutable;

namespace DataStructures.Tests;

public class HeatmapLayoutDataTests
{
    private static readonly OMColor Black = new(0, 0, 0);

    private static OMRect DefaultRect(double left, double top, double width, double height, OMColor? color = null) =>
        new(
            new OMPoint(left, top), new OMPoint(left + width, top),
            new OMPoint(left, top + height), new OMPoint(left + width, top + height),
            width, height, color ?? Black);

    private static Result<HeatmapLayoutData> CreateValid(
        ImmutableArray<OMRect>? cellRects = null,
        ImmutableArray<OMTextBox>? tickLabels = null,
        ImmutableArray<OMTextBox>? axisTitles = null)
    {
        return HeatmapLayoutData.Create(
            xAxis: new OMLine(new OMPoint(0, 100), new OMPoint(100, 100), Black, 2),
            yAxis: new OMLine(new OMPoint(0, 100), new OMPoint(0, 0), Black, 2),
            cellRects: cellRects ?? ImmutableArray.Create(
                DefaultRect(0, 0, 50, 50, new OMColor(255, 0, 0)),
                DefaultRect(50, 0, 50, 50, new OMColor(0, 255, 0))),
            tickLabels: tickLabels ?? ImmutableArray.Create(
                new OMTextBox("0", DefaultRect(0, 104, 50, 20), OMTextRotation.Horizontal),
                new OMTextBox("100", DefaultRect(50, 104, 50, 20), OMTextRotation.Horizontal)),
            axisTitles: axisTitles ?? ImmutableArray.Create(
                new OMTextBox("A", DefaultRect(0, 130, 100, 26), OMTextRotation.Horizontal),
                new OMTextBox("B", DefaultRect(-40, 0, 26, 100), OMTextRotation.CounterClockwise90)));
    }

    [Fact]
    public void Create_succeeds_and_exposes_the_computed_geometry()
    {
        var layout = CreateValid().Should().BeOfType<Success<HeatmapLayoutData>>().Subject.Value;

        layout.CellRects.Should().HaveCount(2);
        layout.CellRects[0].Width.Should().Be(50);
        layout.CellRects[0].Height.Should().Be(50);
        layout.CellRects[0].FillColor.Should().Be(new OMColor(255, 0, 0));
        layout.CellRects[1].FillColor.Should().Be(new OMColor(0, 255, 0));
        layout.XAxis.Should().Be(new OMLine(new OMPoint(0, 100), new OMPoint(100, 100), Black, 2));
        layout.YAxis.Should().Be(new OMLine(new OMPoint(0, 100), new OMPoint(0, 0), Black, 2));
    }

    [Fact]
    public void Create_succeeds_and_exposes_the_tick_labels_and_axis_titles()
    {
        var layout = CreateValid().Should().BeOfType<Success<HeatmapLayoutData>>().Subject.Value;

        layout.TickLabels.Should().HaveCount(2);
        layout.TickLabels[0].Text.Should().Be("0");
        layout.TickLabels[1].Text.Should().Be("100");
        layout.TickLabels.Should().OnlyContain(box => box.Rotation == OMTextRotation.Horizontal);

        layout.AxisTitles.Should().HaveCount(2);
        layout.AxisTitles[0].Text.Should().Be("A");
        layout.AxisTitles[0].Rotation.Should().Be(OMTextRotation.Horizontal);
        layout.AxisTitles[1].Text.Should().Be("B");
        layout.AxisTitles[1].Rotation.Should().Be(OMTextRotation.CounterClockwise90);
    }

    [Fact]
    public void Create_fails_when_a_cell_rects_width_is_not_positive()
    {
        var result = CreateValid(cellRects: ImmutableArray.Create(DefaultRect(0, 0, 0, 50, new OMColor(255, 0, 0))));

        result.Should().BeOfType<Failure<HeatmapLayoutData>>();
    }

    [Fact]
    public void Create_fails_when_a_cell_rects_height_is_not_positive()
    {
        var result = CreateValid(cellRects: ImmutableArray.Create(DefaultRect(0, 0, 50, 0, new OMColor(255, 0, 0))));

        result.Should().BeOfType<Failure<HeatmapLayoutData>>();
    }

    [Fact]
    public void Create_fails_when_a_tick_labels_rect_width_is_not_positive()
    {
        var result = CreateValid(
            tickLabels: ImmutableArray.Create(new OMTextBox("0", DefaultRect(0, 104, 0, 20), OMTextRotation.Horizontal)));

        result.Should().BeOfType<Failure<HeatmapLayoutData>>();
    }

    [Fact]
    public void Create_fails_when_an_axis_titles_rect_height_is_not_positive()
    {
        var result = CreateValid(
            axisTitles: ImmutableArray.Create(new OMTextBox("A", DefaultRect(0, 130, 100, 0), OMTextRotation.Horizontal)));

        result.Should().BeOfType<Failure<HeatmapLayoutData>>();
    }
}
