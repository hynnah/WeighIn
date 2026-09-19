namespace WeighIn.Services;

public static class WaistHipsCalculator
{
    public static double Ratio(double waistCm, double hipsCm) => hipsCm > 0 ? Math.Round(waistCm / hipsCm, 2) : 0;

    public static string GetCategory(double ratio) =>
        ratio <= 0 ? "" : ratio < 0.85 ? "Low risk" : ratio <= 0.90 ? "Moderate risk" : "High risk";

    public static Color GetCategoryColor(double ratio, bool isDark)
    {
        var category = GetCategory(ratio);
        return (category, isDark) switch
        {
            ("Low risk", true) => Color.FromArgb("#7FC39A"),
            ("Low risk", false) => Color.FromArgb("#3D8259"),
            ("Moderate risk", true) => Color.FromArgb("#E8CF95"),
            ("Moderate risk", false) => Color.FromArgb("#A87C2E"),
            ("High risk", true) => Color.FromArgb("#E0A08C"),
            ("High risk", false) => Color.FromArgb("#B15A3E"),
            _ => Color.FromArgb("#75798C")
        };
    }
}
