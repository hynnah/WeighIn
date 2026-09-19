using WeighIn.Services;

namespace WeighIn.Tests;

public class BmiCalculatorTests
{
    [Theory]
    [InlineData(18.4, "Underweight")]
    [InlineData(18.5, "Normal")]
    [InlineData(24.9, "Normal")]
    [InlineData(25.0, "Overweight")]
    [InlineData(29.9, "Overweight")]
    [InlineData(30.0, "Obese")]
    public void GetCategory_GeneralStandard_UsesWhoThresholds(double bmi, string expected)
    {
        Assert.Equal(expected, BmiCalculator.GetCategory(bmi, "General"));
    }

    [Theory]
    [InlineData(18.4, "Underweight")]
    [InlineData(18.5, "Normal")]
    [InlineData(22.9, "Normal")]
    [InlineData(23.0, "Overweight")]
    [InlineData(24.9, "Overweight")]
    [InlineData(25.0, "Obese")]
    public void GetCategory_AsianStandard_UsesTighterThresholds(double bmi, string expected)
    {
        Assert.Equal(expected, BmiCalculator.GetCategory(bmi, "Asian"));
    }

    [Fact]
    public void GetThresholds_GeneralStandard_Returns25And30()
    {
        var (overweight, obese) = BmiCalculator.GetThresholds("General");
        Assert.Equal(25, overweight);
        Assert.Equal(30, obese);
    }

    [Fact]
    public void GetThresholds_AsianStandard_Returns23And25()
    {
        var (overweight, obese) = BmiCalculator.GetThresholds("Asian");
        Assert.Equal(23, overweight);
        Assert.Equal(25, obese);
    }

    [Theory]
    [InlineData(17.0, "Asian", true)]
    [InlineData(17.0, "Asian", false)]
    [InlineData(30.0, "General", true)]
    [InlineData(30.0, "General", false)]
    public void GetCategoryColor_NeverReturnsDefaultTransparent(double bmi, string standard, bool isDark)
    {
        var color = BmiCalculator.GetCategoryColor(bmi, standard, isDark);
        Assert.NotEqual(0, color.Alpha);
    }
}
