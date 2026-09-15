namespace WeighIn.Services;

public static class BmiCalculator
{
    public static string GetCategory(double bmi, string standard) => standard == "General"
        ? bmi < 18.5 ? "Underweight" : bmi < 25 ? "Normal" : bmi < 30 ? "Overweight" : "Obese"
        : bmi < 18.5 ? "Underweight" : bmi < 23 ? "Normal" : bmi < 25 ? "Overweight" : "Obese";
}
