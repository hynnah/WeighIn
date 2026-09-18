namespace WeighIn.Controls;

public sealed class BmiGaugeDrawable : IDrawable
{
    public double Bmi { get; set; }
    public double UnderweightMax { get; set; } = 18.5;
    public double OverweightMax { get; set; } = 23;
    public double ObeseMax { get; set; } = 25;
    public double MinBmi { get; set; } = 16;
    public double MaxBmi { get; set; } = 32;
    public bool IsDark { get; set; }
    public Color MarkerFillColor { get; set; } = Colors.White;
    public Color MarkerRingColor { get; set; } = Color.FromArgb("#182C2B");

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float strokeWidth = 8f;
        var padding = strokeWidth / 2 + 2;
        var radius = Math.Min(dirtyRect.Width / 2, dirtyRect.Height) - padding;
        if (radius <= 0)
            return;

        var centerX = dirtyRect.Width / 2;
        var centerY = dirtyRect.Height - padding;

        canvas.StrokeSize = strokeWidth;
        canvas.StrokeLineCap = LineCap.Round;

        var underFraction = Fraction(UnderweightMax);
        var overFraction = Fraction(OverweightMax);
        var obeseFraction = Fraction(ObeseMax);

        var underweightColor = IsDark ? Color.FromArgb("#3C4F74") : Color.FromArgb("#9DB8DA");
        var normalColor = IsDark ? Color.FromArgb("#33603F") : Color.FromArgb("#9BC7AC");
        var overweightColor = IsDark ? Color.FromArgb("#6A5227") : Color.FromArgb("#D9BD7E");
        var obeseColor = IsDark ? Color.FromArgb("#6D3A2E") : Color.FromArgb("#D9A48F");

        DrawBand(canvas, centerX, centerY, radius, 0, underFraction, underweightColor);
        DrawBand(canvas, centerX, centerY, radius, underFraction, overFraction, normalColor);
        DrawBand(canvas, centerX, centerY, radius, overFraction, obeseFraction, overweightColor);
        DrawBand(canvas, centerX, centerY, radius, obeseFraction, 1, obeseColor);

        if (Bmi <= 0)
            return;

        var markerFraction = (float)Fraction(Bmi);
        var angle = Math.PI * (1 - markerFraction);
        var markerX = centerX + radius * (float)Math.Cos(angle);
        var markerY = centerY - radius * (float)Math.Sin(angle);

        const float markerRadius = 6f;
        canvas.FillColor = MarkerFillColor;
        canvas.FillEllipse(markerX - markerRadius, markerY - markerRadius, markerRadius * 2, markerRadius * 2);
        canvas.StrokeColor = MarkerRingColor;
        canvas.StrokeSize = 2.5f;
        canvas.DrawEllipse(markerX - markerRadius, markerY - markerRadius, markerRadius * 2, markerRadius * 2);
    }

    private double Fraction(double bmi) => Math.Clamp((bmi - MinBmi) / (MaxBmi - MinBmi), 0, 1);

    private static void DrawBand(ICanvas canvas, float centerX, float centerY, float radius, double fromFraction, double toFraction, Color color)
    {
        if (toFraction <= fromFraction)
            return;

        canvas.StrokeColor = color;
        const int segments = 24;
        var path = new PathF();

        for (var index = 0; index <= segments; index++)
        {
            var t = fromFraction + (toFraction - fromFraction) * index / segments;
            var angle = Math.PI * (1 - t);
            var x = centerX + radius * (float)Math.Cos(angle);
            var y = centerY - radius * (float)Math.Sin(angle);

            if (index == 0)
                path.MoveTo(x, y);
            else
                path.LineTo(x, y);
        }

        canvas.DrawPath(path);
    }
}
