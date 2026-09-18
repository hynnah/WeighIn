namespace WeighIn.Services;

public static class BmiCalculator
{
    public static string GetCategory(double bmi, string standard) => standard == "General"
        ? bmi < 18.5 ? "Underweight" : bmi < 25 ? "Normal" : bmi < 30 ? "Overweight" : "Obese"
        : bmi < 18.5 ? "Underweight" : bmi < 23 ? "Normal" : bmi < 25 ? "Overweight" : "Obese";

    /// <summary>Category color. Dark-theme values match the reference design exactly; light-theme values are
    /// darkened/more-saturated variants of the same hues since the reference's pastels are tuned for a dark card.</summary>
    public static Color GetCategoryColor(double bmi, string standard, bool isDark)
    {
        var category = GetCategory(bmi, standard);
        return (category, isDark) switch
        {
            ("Underweight", true) => Color.FromArgb("#7FA8D6"),
            ("Underweight", false) => Color.FromArgb("#3B6FA0"),
            ("Normal", true) => Color.FromArgb("#7FC39A"),
            ("Normal", false) => Color.FromArgb("#3D8259"),
            ("Overweight", true) => Color.FromArgb("#E8CF95"),
            ("Overweight", false) => Color.FromArgb("#A87C2E"),
            (_, true) => Color.FromArgb("#E0A08C"),
            (_, false) => Color.FromArgb("#B15A3E")
        };
    }

    /// <summary>Overweight/obese cutoffs for the given standard, used to position band boundaries on the gauge.</summary>
    public static (double Overweight, double Obese) GetThresholds(string standard) =>
        standard == "General" ? (25, 30) : (23, 25);
}
