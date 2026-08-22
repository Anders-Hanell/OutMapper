using System.Collections.Immutable;

namespace DataStructures.Tests;

public class HeatmapLayoutDataTests
{
    private static OMRect DefaultRect(double left, double top, double width, double height, OMColor color) =>
        new(
            new OMPoint(left, top), new OMPoint(left + width, top),
            new OMPoint(left, top + height), new OMPoint(left + width, top + height),
            width, height, color);

    private static Result<HeatmapLayoutData> CreateValid(
        ImmutableArray<OMRect>? cellRects = null,
        ImmutableArray<double>? xTickX = null, ImmutableArray<string>? xTickText = null,
        ImmutableArray<double>? yTickY = null, ImmutableArray<string>? yTickText = null)
    {
        var black = new OMColor(0, 0, 0);
        return HeatmapLayoutData.Create(
            xAxis: new OMLine(new OMPoint(0, 100), new OMPoint(100, 100), black, 2),
            yAxis: new OMLine(new OMPoint(0, 100), new OMPoint(0, 0), black, 2),
            cellRects: cellRects ?? ImmutableArray.Create(
                DefaultRect(0, 0, 50, 50, new OMColor(255, 0, 0)),
                DefaultRect(50, 0, 50, 50, new OMColor(0, 255, 0))),
            xTickX: xTickX ?? ImmutableArray.Create(0.0, 50.0, 100.0),
            xTickText: xTickText ?? ImmutableArray.Create("0", "50", "100"),
            xTickY: 114,
            yTickY: yTickY ?? ImmutableArray.Create(4.0, 104.0),
            yTickText: yTickText ?? ImmutableArray.Create("0", "100"),
            yTickX: -6,
            xAxisTitleText: "A", xAxisTitleX: 50, xAxisTitleY: 136,
            yAxisTitleText: "B", yAxisTitleX: -40, yAxisTitleY: 50);
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
        layout.XAxis.Should().Be(new OMLine(new OMPoint(0, 100), new OMPoint(100, 100), new OMColor(0, 0, 0), 2));
        layout.YAxis.Should().Be(new OMLine(new OMPoint(0, 100), new OMPoint(0, 0), new OMColor(0, 0, 0), 2));
        layout.XAxisTitleText.Should().Be("A");
        layout.YAxisTitleText.Should().Be("B");
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
    public void Create_fails_when_x_tick_position_and_text_lengths_differ()
    {
        var result = CreateValid(xTickText: ImmutableArray.Create("0", "50"));

        result.Should().BeOfType<Failure<HeatmapLayoutData>>();
    }

    [Fact]
    public void Create_fails_when_y_tick_position_and_text_lengths_differ()
    {
        var result = CreateValid(yTickText: ImmutableArray.Create("0"));

        result.Should().BeOfType<Failure<HeatmapLayoutData>>();
    }
}
