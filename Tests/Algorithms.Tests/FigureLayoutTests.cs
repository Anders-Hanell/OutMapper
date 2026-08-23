using System.Collections.Immutable;
using DataStructures;

namespace Algorithms.Tests;

public class FigureLayoutTests
{
    private static GraphDrawData GraphNoChrome() =>
        GraphDrawData.Create(
            "ICP", "PRx", ImmutableArray.Create(0.0, 10.0), ImmutableArray.Create(-1.0, 1.0),
            ImmutableArray.Create("#FF0000"), drawAxisTickLabels: false, drawAxisTitles: false)
            .Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

    private static GraphDrawData GraphWithChrome() =>
        GraphDrawData.Create(
            "ICP", "PRx", ImmutableArray.Create(0.0, 10.0), ImmutableArray.Create(-1.0, 1.0),
            ImmutableArray.Create("#FF0000"), drawAxisTickLabels: true, drawAxisTitles: true)
            .Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

    private static FigureDrawData CreateFigure(
        int rowCount, int colCount, IReadOnlyList<bool> cellHasGraph, IReadOnlyList<GraphDrawData> graphs,
        FigureLabelStyle labelStyle = FigureLabelStyle.None) =>
        FigureDrawData.Create(rowCount, colCount, cellHasGraph, graphs, labelStyle)
            .Should().BeOfType<Success<FigureDrawData>>().Subject.Value;

    // A HeatmapLayoutData doesn't expose its target area as a single rect, but its Lines
    // (built from areaLeft/areaTop/areaWidth/areaHeight by Algorithms.HeatmapLayout.Compute, in the
    // fixed order X axis then Y axis) pin it down: the Y axis runs from (areaLeft, areaTop + areaHeight)
    // up to (areaLeft, areaTop).
    private static OMLine XAxis(HeatmapLayoutData layout) => layout.Lines[0];

    private static OMLine YAxis(HeatmapLayoutData layout) => layout.Lines[1];

    private static OMPoint AreaTopLeft(HeatmapLayoutData layout) => YAxis(layout).End;

    private static double AreaWidth(HeatmapLayoutData layout) => XAxis(layout).End.X - XAxis(layout).Start.X;

    private static double AreaHeight(HeatmapLayoutData layout) => YAxis(layout).Start.Y - YAxis(layout).End.Y;

    [Fact]
    public void Compute_lays_the_grid_out_onto_a_fixed_size_page()
    {
        var figure = CreateFigure(rowCount: 1, colCount: 1, cellHasGraph: [true], graphs: [GraphNoChrome()]);

        var layout = FigureLayout.Compute(figure).Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        layout.PageWidth.Should().Be(612);
        layout.PageHeight.Should().Be(792);
    }

    [Fact]
    public void Compute_produces_one_heatmap_layout_per_graph_cell_regardless_of_empty_cells()
    {
        var figure = CreateFigure(
            rowCount: 2, colCount: 2,
            cellHasGraph: [true, false, false, true],
            graphs: [GraphNoChrome(), GraphNoChrome()]);

        var layout = FigureLayout.Compute(figure).Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        layout.HeatmapLayouts.Should().HaveCount(2);
    }

    [Fact]
    public void Compute_sizes_and_positions_cells_across_the_grid_with_gaps_between_them()
    {
        var figure = CreateFigure(
            rowCount: 1, colCount: 2,
            cellHasGraph: [true, true],
            graphs: [GraphNoChrome(), GraphNoChrome()]);

        var layout = FigureLayout.Compute(figure).Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        // The grid area is a fixed 360x360 square with a 16-wide gap: colCount=2 gives
        // cellOuterWidth = (360 - 16) / 2 = 172; rowCount=1 gives cellOuterHeight = 360.
        layout.HeatmapLayouts.Should().HaveCount(2);
        AreaWidth(layout.HeatmapLayouts[0]).Should().Be(172);
        AreaHeight(layout.HeatmapLayouts[0]).Should().Be(360);
        AreaTopLeft(layout.HeatmapLayouts[0]).Should().Be(new OMPoint(112, 336));
        AreaTopLeft(layout.HeatmapLayouts[1]).Should().Be(new OMPoint(300, 336));
    }

    [Fact]
    public void Compute_shrinks_each_cells_heatmap_area_by_its_own_reserved_margins_and_the_label_height()
    {
        var figure = CreateFigure(
            rowCount: 1, colCount: 1, cellHasGraph: [true], graphs: [GraphWithChrome()],
            labelStyle: FigureLabelStyle.Uppercase);

        var layout = FigureLayout.Compute(figure).Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        // A 1x1 grid's cell fills the whole 360x360 area at (112, 336). GraphWithChrome reserves
        // left=52 (26 ticks + 26 title), bottom=46 (20 ticks + 26 title); the label strip reserves
        // another 16 off the top.
        var heatmapLayout = layout.HeatmapLayouts.Single();
        AreaTopLeft(heatmapLayout).Should().Be(new OMPoint(112 + 52, 336 + 16));
        AreaWidth(heatmapLayout).Should().Be(360 - 52);
        AreaHeight(heatmapLayout).Should().Be(360 - 16 - 46);
    }

    [Fact]
    public void Compute_does_not_reserve_label_height_when_the_figure_has_no_label_style()
    {
        var figure = CreateFigure(
            rowCount: 1, colCount: 1, cellHasGraph: [true], graphs: [GraphNoChrome()]);

        var layout = FigureLayout.Compute(figure).Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        AreaTopLeft(layout.HeatmapLayouts.Single()).Should().Be(new OMPoint(112, 336));
        layout.Labels.Should().BeEmpty();
    }

    [Fact]
    public void Compute_embeds_each_graphs_own_tick_labels_and_axis_titles_when_enabled()
    {
        var figure = CreateFigure(
            rowCount: 1, colCount: 1, cellHasGraph: [true], graphs: [GraphWithChrome()]);

        var layout = FigureLayout.Compute(figure).Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        // GraphWithChrome has ticks and titles enabled, so the embedded HeatmapLayoutData should carry
        // its own fully computed cell colors, tick labels, and axis titles - not just a bare target rect.
        var heatmapLayout = layout.HeatmapLayouts.Single();
        heatmapLayout.CellRects.Should().NotBeEmpty();
        // HeatmapLayout.Compute always emits exactly two groups, in this fixed order: ticks, then titles.
        heatmapLayout.TextGroups[0].Boxes.Should().NotBeEmpty();
        heatmapLayout.TextGroups[1].Boxes.Select(box => box.Text).Should().Equal("ICP", "PRx");
    }

    [Fact]
    public void Compute_places_the_label_strip_at_the_top_of_the_cell()
    {
        var figure = CreateFigure(
            rowCount: 1, colCount: 1, cellHasGraph: [true], graphs: [GraphNoChrome()],
            labelStyle: FigureLabelStyle.Uppercase);

        var layout = FigureLayout.Compute(figure).Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        var label = layout.Labels.Single();
        label.Rect.TopLeft.Should().Be(new OMPoint(112, 336));
        label.Rect.Width.Should().Be(360);
        label.Rect.Height.Should().Be(16);
        label.Rotation.Should().Be(OMTextRotation.Horizontal);
    }

    [Fact]
    public void Compute_labels_every_cell_in_one_continuous_row_major_sequence_including_empty_cells()
    {
        // Row-major cell order is graph, empty, graph, graph, and Labels follows that same order
        // regardless of which cells have a graph - so it reads A, B, C, D straight down the grid.
        var figure = CreateFigure(
            rowCount: 2, colCount: 2,
            cellHasGraph: [true, false, true, true],
            graphs: [GraphNoChrome(), GraphNoChrome(), GraphNoChrome()],
            labelStyle: FigureLabelStyle.Uppercase);

        var layout = FigureLayout.Compute(figure).Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        layout.Labels.Select(box => box.Text).Should().Equal("A", "B", "C", "D");
    }

    [Fact]
    public void Compute_places_an_empty_cells_label_strip_at_the_top_of_that_cell()
    {
        var figure = CreateFigure(
            rowCount: 1, colCount: 1, cellHasGraph: [false], graphs: [],
            labelStyle: FigureLabelStyle.Uppercase);

        var layout = FigureLayout.Compute(figure).Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        var label = layout.Labels.Single();
        label.Text.Should().Be("A");
        label.Rect.TopLeft.Should().Be(new OMPoint(112, 336));
        label.Rect.Width.Should().Be(360);
        label.Rect.Height.Should().Be(16);
        label.Rotation.Should().Be(OMTextRotation.Horizontal);
    }

    [Fact]
    public void Compute_lowercases_labels_when_the_label_style_is_lowercase()
    {
        var figure = CreateFigure(
            rowCount: 1, colCount: 1, cellHasGraph: [true], graphs: [GraphNoChrome()],
            labelStyle: FigureLabelStyle.Lowercase);

        var layout = FigureLayout.Compute(figure).Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        layout.Labels.Single().Text.Should().Be("a");
    }

    [Fact]
    public void Compute_wraps_labels_past_z_to_aa()
    {
        // 9x3 keeps every cell rect positive within the fixed 360x360 grid area while still giving
        // 27 graph cells to push the label index past the single-letter range.
        var graphs = Enumerable.Range(0, 27).Select(_ => GraphNoChrome()).ToList();
        var figure = CreateFigure(
            rowCount: 9, colCount: 3, cellHasGraph: Enumerable.Repeat(true, 27).ToList(), graphs: graphs,
            labelStyle: FigureLabelStyle.Uppercase);

        var layout = FigureLayout.Compute(figure).Should().BeOfType<Success<FigureLayoutData>>().Subject.Value;

        layout.Labels[25].Text.Should().Be("Z");
        layout.Labels[26].Text.Should().Be("AA");
    }

    [Fact]
    public void Compute_fails_when_the_grid_has_too_many_cells_to_fit_the_fixed_page_area()
    {
        // With no HeatmapLayouts to validate (every cell is empty), a positive-size check needs a
        // FigureLabelStyle so the too-small label rects are what surfaces the failure.
        var figure = CreateFigure(
            rowCount: 1, colCount: 30, cellHasGraph: Enumerable.Repeat(false, 30).ToList(),
            graphs: Array.Empty<GraphDrawData>(), labelStyle: FigureLabelStyle.Uppercase);

        var result = FigureLayout.Compute(figure);

        result.Should().BeOfType<Failure<FigureLayoutData>>();
    }

    [Fact]
    public void Compute_fails_when_a_cells_reserved_chrome_margins_leave_no_room_for_its_heatmap()
    {
        // cellOuterWidth = (360 - 6*16) / 7 = 37.71, but GraphWithChrome reserves 52 off the left alone.
        var figure = CreateFigure(
            rowCount: 1, colCount: 7,
            cellHasGraph: [true, false, false, false, false, false, false],
            graphs: [GraphWithChrome()]);

        var result = FigureLayout.Compute(figure);

        result.Should().BeOfType<Failure<FigureLayoutData>>();
    }
}
