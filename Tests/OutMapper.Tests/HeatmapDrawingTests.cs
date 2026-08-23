using System.Collections.Immutable;
using DataStructures;
using SkiaSharp;
using TestSupport;

namespace OutMapper.Tests;

public class HeatmapDrawingTests
{
    [Fact]
    public async Task Draw_fills_each_cell_with_its_configured_color()
    {
        GatewayTestHarness.EnsureInitialized();
        using var surface = SKSurface.Create(new SKImageInfo(200, 200));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var data = GraphDrawData.Create(
            channelAName: "A",
            channelBName: "B",
            channelABinEdges: ImmutableArray.Create(0.0, 1.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 1.0),
            cellColorsRowMajor: ImmutableArray.Create("#FF0000"),
            drawAxisTickLabels: false,
            drawAxisTitles: false).Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

        await HeatmapDrawing.DrawAsync(canvas, new SKRect(0, 0, 100, 100), data);

        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);
        var centerPixel = bitmap.GetPixel(50, 50);

        centerPixel.Should().Be(new SKColor(0xFF, 0x00, 0x00));
    }

    [Fact]
    public async Task Draw_leaves_a_cell_white_when_its_color_is_empty()
    {
        GatewayTestHarness.EnsureInitialized();
        using var surface = SKSurface.Create(new SKImageInfo(200, 200));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Black);

        var data = GraphDrawData.Create(
            channelAName: "A",
            channelBName: "B",
            channelABinEdges: ImmutableArray.Create(0.0, 1.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 1.0),
            cellColorsRowMajor: ImmutableArray.Create(string.Empty),
            drawAxisTickLabels: false,
            drawAxisTitles: false).Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

        await HeatmapDrawing.DrawAsync(canvas, new SKRect(0, 0, 100, 100), data);

        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);
        var centerPixel = bitmap.GetPixel(50, 50);

        centerPixel.Should().Be(SKColors.White);
    }

    [Fact]
    public async Task Draw_does_not_throw_and_still_fills_cells_when_tick_labels_and_axis_titles_are_enabled()
    {
        GatewayTestHarness.EnsureInitialized();
        using var surface = SKSurface.Create(new SKImageInfo(300, 300));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var data = GraphDrawData.Create(
            channelAName: "A",
            channelBName: "B",
            channelABinEdges: ImmutableArray.Create(0.0, 1.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 1.0),
            cellColorsRowMajor: ImmutableArray.Create("#FF0000"),
            drawAxisTickLabels: true,
            drawAxisTitles: true).Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

        var act = async () => await HeatmapDrawing.DrawAsync(canvas, new SKRect(60, 20, 260, 220), data);
        await act.Should().NotThrowAsync();

        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);

        bitmap.GetPixel(160, 120).Should().Be(new SKColor(0xFF, 0x00, 0x00));
    }
}
