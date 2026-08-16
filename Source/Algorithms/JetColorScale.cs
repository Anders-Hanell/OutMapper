namespace Algorithms;

public static class JetColorScale
{
    // Matches the R reference implementation's jet.color.values, standard order (low -> high).
    private static readonly (byte R, byte G, byte B)[] Anchors =
    {
        (0x00, 0x00, 0x7F),
        (0x00, 0x00, 0xFF),
        (0x00, 0x7F, 0xFF),
        (0x00, 0xFF, 0xFF),
        (0x7F, 0xFF, 0x7F),
        (0xFF, 0xFF, 0x00),
        (0xFF, 0x7F, 0x00),
        (0xFF, 0x00, 0x00),
        (0x7F, 0x00, 0x00)
    };

    /// <summary>
    /// Maps a value in [<paramref name="minValue"/>, <paramref name="maxValue"/>] (clamped) to a Jet
    /// color, piecewise-linearly interpolated in RGB space across the anchor colors. Returns null for
    /// a null value, so callers can render NA cells as white.
    /// </summary>
    public static string? ToHexColor(double? value, double minValue, double maxValue)
    {
        if (value is not double actualValue)
        {
            return null;
        }

        var clamped = Math.Clamp(actualValue, minValue, maxValue);
        var t = maxValue > minValue ? (clamped - minValue) / (maxValue - minValue) : 0.0;

        var segmentCount = Anchors.Length - 1;
        var scaledPosition = t * segmentCount;
        var segmentIndex = Math.Min((int)Math.Floor(scaledPosition), segmentCount - 1);
        var segmentFraction = scaledPosition - segmentIndex;

        var start = Anchors[segmentIndex];
        var end = Anchors[segmentIndex + 1];

        var r = (byte)Math.Round(start.R + (end.R - start.R) * segmentFraction);
        var g = (byte)Math.Round(start.G + (end.G - start.G) * segmentFraction);
        var b = (byte)Math.Round(start.B + (end.B - start.B) * segmentFraction);

        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
