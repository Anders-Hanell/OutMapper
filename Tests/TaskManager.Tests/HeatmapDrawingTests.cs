using System.Collections.Immutable;
using DataStructures;
using SkiaSharp;

namespace TaskManager.Tests;

public class HeatmapDrawingTests
{
    [Fact]
    public void Draw_fills_each_cell_with_its_configured_color()
    {
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

        HeatmapDrawing.Draw(canvas, new SKRect(0, 0, 100, 100), data);

        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);
        var centerPixel = bitmap.GetPixel(50, 50);

        centerPixel.Should().Be(new SKColor(0xFF, 0x00, 0x00));
    }

    [Fact]
    public void Draw_leaves_a_cell_white_when_its_color_is_empty()
    {
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

        HeatmapDrawing.Draw(canvas, new SKRect(0, 0, 100, 100), data);

        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);
        var centerPixel = bitmap.GetPixel(50, 50);

        centerPixel.Should().Be(SKColors.White);
    }

    [Fact]
    public void Draw_does_not_throw_and_still_fills_cells_when_tick_labels_and_axis_titles_are_enabled()
    {
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

        var act = () => HeatmapDrawing.Draw(canvas, new SKRect(60, 20, 260, 220), data);
        act.Should().NotThrow();

        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);

        bitmap.GetPixel(160, 120).Should().Be(new SKColor(0xFF, 0x00, 0x00));
    }
}
