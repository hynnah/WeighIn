using WeighIn.Services;

namespace WeighIn.Tests;

public class WaistHipsCalculatorTests
{
    [Fact]
    public void Ratio_RoundsToTwoDecimalPlaces()
    {
        Assert.Equal(0.80, WaistHipsCalculator.Ratio(78.5, 98.0));
    }

    [Fact]
    public void Ratio_ZeroHips_ReturnsZeroInsteadOfDividingByZero()
    {
        Assert.Equal(0, WaistHipsCalculator.Ratio(78.5, 0));
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(0.84, "Low risk")]
    [InlineData(0.85, "Moderate risk")]
    [InlineData(0.90, "Moderate risk")]
    [InlineData(0.91, "High risk")]
    public void GetCategory_MatchesSimplifiedRiskBands(double ratio, string expected)
    {
        Assert.Equal(expected, WaistHipsCalculator.GetCategory(ratio));
    }

    [Fact]
    public void GetCategoryColor_EmptyCategory_ReturnsMutedFallback()
    {
        var color = WaistHipsCalculator.GetCategoryColor(0, isDark: true);
        Assert.Equal(Microsoft.Maui.Graphics.Color.FromArgb("#75798C"), color);
    }

    [Theory]
    [InlineData(0.80, true, "#7FC39A")]
    [InlineData(0.80, false, "#3D8259")]
    [InlineData(0.95, true, "#E0A08C")]
    [InlineData(0.95, false, "#B15A3E")]
    public void GetCategoryColor_MatchesThemeVariant(double ratio, bool isDark, string expectedHex)
    {
        var color = WaistHipsCalculator.GetCategoryColor(ratio, isDark);
        Assert.Equal(Microsoft.Maui.Graphics.Color.FromArgb(expectedHex), color);
    }
}
