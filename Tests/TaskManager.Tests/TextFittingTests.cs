using SkiaSharp;

namespace TaskManager.Tests;

public class TextFittingTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FitSize_returns_zero_for_null_or_empty_or_whitespace_text(string? text)
    {
        TextFitting.FitSize(text, maxWidth: 100, maxHeight: 100).Should().Be(0f);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(-1, 100)]
    [InlineData(100, 0)]
    [InlineData(100, -1)]
    public void FitSize_returns_zero_when_width_or_height_budget_is_not_positive(float maxWidth, float maxHeight)
    {
        TextFitting.FitSize("Hello", maxWidth, maxHeight).Should().Be(0f);
    }

    [Fact]
    public void FitSize_matches_the_hand_computed_linear_scale()
    {
        const string text = "Hello";
        using var probeFont = new SKFont(null, 100f);
        probeFont.MeasureText(text, out var bounds);

        var fitted = TextFitting.FitSize(text, maxWidth: bounds.Width / 2f, maxHeight: 10_000f);

        fitted.Should().BeApproximately(50f, 0.01f);
    }

    [Fact]
    public void FitSize_gives_a_longer_string_a_smaller_fitted_size_than_a_shorter_one_in_the_same_box()
    {
        var shortSize = TextFitting.FitSize("A", maxWidth: 200, maxHeight: 200);
        var longSize = TextFitting.FitSize("A very very long axis title", maxWidth: 200, maxHeight: 200);

        longSize.Should().BeLessThan(shortSize);
    }

    [Fact]
    public void FitUniformSize_returns_the_minimum_across_items()
    {
        var items = new (string Text, float MaxWidth, float MaxHeight)[]
        {
            ("A", 200, 200),
            ("A much longer piece of text", 200, 200),
            ("Medium text", 200, 200)
        };

        var expected = items.Min(item => TextFitting.FitSize(item.Text, item.MaxWidth, item.MaxHeight));

        TextFitting.FitUniformSize(items).Should().Be(expected);
    }

    [Fact]
    public void FitUniformSize_returns_zero_for_an_empty_collection()
    {
        TextFitting.FitUniformSize(Array.Empty<(string, float, float)>()).Should().Be(0f);
    }
}
