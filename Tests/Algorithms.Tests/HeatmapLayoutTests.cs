using System.Collections.Immutable;
using DataStructures;

namespace Algorithms.Tests;

public class HeatmapLayoutTests
{
    private static GraphDrawData CreateGraphData(
        ImmutableArray<double>? channelABinEdges = null, ImmutableArray<double>? channelBBinEdges = null,
        bool drawAxisTickLabels = false, bool drawAxisTitles = false, ImmutableArray<string>? cellColorsRowMajor = null)
    {
        var aEdges = channelABinEdges ?? ImmutableArray.Create(0.0, 50.0, 100.0);
        var bEdges = channelBBinEdges ?? ImmutableArray.Create(0.0, 50.0, 100.0);
        var cellCount = (aEdges.Length - 1) * (bEdges.Length - 1);

        return GraphDrawData.Create(
            "ICP", "PRx", aEdges, bEdges,
            cellColorsRowMajor ?? Enumerable.Repeat("#FF0000", cellCount).ToImmutableArray(),
            drawAxisTickLabels, drawAxisTitles).Should().BeOfType<Success<GraphDrawData>>().Subject.Value;
    }

    [Fact]
    public void Compute_places_row_zero_at_the_bottom_of_the_area()
    {
        var data = CreateGraphData(
            channelABinEdges: ImmutableArray.Create(0.0, 50.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 50.0, 100.0));

        var layout = HeatmapLayout.Compute(data, areaLeft: 0, areaTop: 0, areaWidth: 50, areaHeight: 100)
            .Should().BeOfType<Success<HeatmapLayoutData>>().Subject.Value;

        // Row 0 (index 0) is the lowest bin, so it's drawn at the bottom (top = areaHeight - cellHeight).
        layout.CellRects[0].TopLeft.Y.Should().Be(50);
        // Row 1 is drawn at the top of the area.
        layout.CellRects[1].TopLeft.Y.Should().Be(0);
    }

    [Fact]
    public void Compute_sizes_each_cell_rect_to_the_area_divided_by_the_grid()
    {
        var data = CreateGraphData(
            channelABinEdges: ImmutableArray.Create(0.0, 50.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 50.0));

        var layout = HeatmapLayout.Compute(data, areaLeft: 10, areaTop: 20, areaWidth: 50, areaHeight: 25)
            .Should().BeOfType<Success<HeatmapLayoutData>>().Subject.Value;

        var rect = layout.CellRects.Single();
        rect.Width.Should().Be(50);
        rect.Height.Should().Be(25);
        rect.TopLeft.Should().Be(new OMPoint(10, 20));
        rect.TopRight.Should().Be(new OMPoint(60, 20));
        rect.BottomLeft.Should().Be(new OMPoint(10, 45));
        rect.BottomRight.Should().Be(new OMPoint(60, 45));
    }

    [Fact]
    public void Compute_places_the_x_axis_along_the_bottom_and_the_y_axis_along_the_left()
    {
        var data = CreateGraphData();

        var layout = HeatmapLayout.Compute(data, areaLeft: 10, areaTop: 20, areaWidth: 100, areaHeight: 50)
            .Should().BeOfType<Success<HeatmapLayoutData>>().Subject.Value;

        var black = new OMColor(0, 0, 0);
        layout.XAxis.Should().Be(new OMLine(new OMPoint(10, 70), new OMPoint(110, 70), black, 2));
        layout.YAxis.Should().Be(new OMLine(new OMPoint(10, 70), new OMPoint(10, 20), black, 2));
    }

    [Fact]
    public void Compute_parses_each_cells_hex_color_and_renders_an_empty_one_as_white()
    {
        var data = CreateGraphData(
            channelABinEdges: ImmutableArray.Create(0.0, 50.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 50.0),
            cellColorsRowMajor: ImmutableArray.Create("#2166AC"));

        var layout = HeatmapLayout.Compute(data, areaLeft: 0, areaTop: 0, areaWidth: 50, areaHeight: 50)
            .Should().BeOfType<Success<HeatmapLayoutData>>().Subject.Value;

        layout.CellRects.Select(rect => rect.FillColor).Should().Equal(new OMColor(0x21, 0x66, 0xAC));
    }

    [Fact]
    public void Compute_renders_an_empty_cell_color_as_white()
    {
        var data = CreateGraphData(
            channelABinEdges: ImmutableArray.Create(0.0, 50.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 50.0),
            cellColorsRowMajor: ImmutableArray.Create(""));

        var layout = HeatmapLayout.Compute(data, areaLeft: 0, areaTop: 0, areaWidth: 50, areaHeight: 50)
            .Should().BeOfType<Success<HeatmapLayoutData>>().Subject.Value;

        layout.CellRects.Select(rect => rect.FillColor).Should().Equal(new OMColor(255, 255, 255));
    }

    private static double CenterX(OMRect rect) => rect.TopLeft.X + rect.Width / 2.0;

    private static double CenterY(OMRect rect) => rect.TopLeft.Y + rect.Height / 2.0;

    [Fact]
    public void Compute_returns_no_ticks_or_titles_when_both_flags_are_false()
    {
        var data = CreateGraphData(drawAxisTickLabels: false, drawAxisTitles: false);

        var layout = HeatmapLayout.Compute(data, areaLeft: 0, areaTop: 0, areaWidth: 100, areaHeight: 100)
            .Should().BeOfType<Success<HeatmapLayoutData>>().Subject.Value;

        layout.TickLabels.Should().BeEmpty();
        layout.AxisTitles.Should().BeEmpty();
    }

    [Fact]
    public void Compute_places_one_x_tick_per_bin_edge_with_formatted_text_centered_on_the_column_boundary()
    {
        var data = CreateGraphData(
            channelABinEdges: ImmutableArray.Create(0.0, 1.5, 3.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 50.0),
            drawAxisTickLabels: true);

        var layout = HeatmapLayout.Compute(data, areaLeft: 0, areaTop: 0, areaWidth: 90, areaHeight: 50)
            .Should().BeOfType<Success<HeatmapLayoutData>>().Subject.Value;

        // X ticks are built (and appear) before Y ticks; 3 column-boundary ticks then 2 row-boundary ticks.
        var xTicks = layout.TickLabels.Take(3).ToImmutableArray();
        xTicks.Select(box => box.Text).Should().Equal("0", "1.5", "3");
        xTicks.Select(box => CenterX(box.Rect)).Should().Equal(0, 45, 90);
        xTicks.Should().OnlyContain(box => box.Rotation == OMTextRotation.Horizontal);
    }

    [Fact]
    public void Compute_sets_axis_titles_from_the_channel_names_when_enabled()
    {
        var data = CreateGraphData(drawAxisTitles: true);

        var layout = HeatmapLayout.Compute(data, areaLeft: 0, areaTop: 0, areaWidth: 100, areaHeight: 100)
            .Should().BeOfType<Success<HeatmapLayoutData>>().Subject.Value;

        layout.AxisTitles.Should().HaveCount(2);
        layout.AxisTitles[0].Text.Should().Be("ICP");
        layout.AxisTitles[0].Rotation.Should().Be(OMTextRotation.Horizontal);
        layout.AxisTitles[1].Text.Should().Be("PRx");
        layout.AxisTitles[1].Rotation.Should().Be(OMTextRotation.CounterClockwise90);
    }

    [Fact]
    public void Compute_sizes_text_box_footprints_matching_the_reserved_margin_constants()
    {
        var data = CreateGraphData(
            channelABinEdges: ImmutableArray.Create(0.0, 50.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 50.0),
            drawAxisTickLabels: true, drawAxisTitles: true);

        var layout = HeatmapLayout.Compute(data, areaLeft: 0, areaTop: 0, areaWidth: 100, areaHeight: 100)
            .Should().BeOfType<Success<HeatmapLayoutData>>().Subject.Value;

        // X ticks (indices 0-1) come before Y ticks (indices 2-3).
        layout.TickLabels.Should().HaveCount(4);
        layout.TickLabels[0].Rect.Height.Should().Be(20);
        layout.TickLabels[1].Rect.Height.Should().Be(20);
        layout.TickLabels[2].Rect.Width.Should().Be(26);
        layout.TickLabels[3].Rect.Width.Should().Be(26);

        layout.AxisTitles.Should().HaveCount(2);
        layout.AxisTitles[0].Rect.Height.Should().Be(26);
        layout.AxisTitles[1].Rect.Width.Should().Be(26);
    }

    [Fact]
    public void Compute_fails_when_the_area_has_no_width_or_height()
    {
        var data = CreateGraphData();

        var result = HeatmapLayout.Compute(data, areaLeft: 0, areaTop: 0, areaWidth: 0, areaHeight: 100);

        result.Should().BeOfType<Failure<HeatmapLayoutData>>();
    }

    [Theory]
    [InlineData(false, false, 0, 0)]
    [InlineData(true, false, 26, 20)]
    [InlineData(false, true, 26, 26)]
    [InlineData(true, true, 52, 46)]
    public void ComputeReservedMargins_sums_the_margins_for_ticks_and_titles(
        bool drawAxisTickLabels, bool drawAxisTitles, double expectedLeft, double expectedBottom)
    {
        var data = CreateGraphData(drawAxisTickLabels: drawAxisTickLabels, drawAxisTitles: drawAxisTitles);

        var (reservedLeft, reservedBottom) = HeatmapLayout.ComputeReservedMargins(data);

        reservedLeft.Should().Be(expectedLeft);
        reservedBottom.Should().Be(expectedBottom);
    }
}
