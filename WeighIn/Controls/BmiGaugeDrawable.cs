namespace WeighIn.Controls;

public sealed class BmiGaugeDrawable : IDrawable
{
    private static readonly Color GradientStart = Color.FromArgb("#3C6EA8");
    private static readonly Color GradientMid = Color.FromArgb("#7B62A8");
    private static readonly Color GradientEnd = Color.FromArgb("#B8412C");

    public double Bmi { get; set; }
    public double MinBmi { get; set; } = 16;
    public double MaxBmi { get; set; } = 32;
    public bool IsDark { get; set; }
    public bool HasEntry { get; set; } = true;
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

        var trackColor = IsDark ? Color.FromArgb("#292B31") : Color.FromArgb("#E4E1D8");
        canvas.StrokeColor = trackColor;
        DrawArcSegment(canvas, centerX, centerY, radius, 0, 1);

        const int gradientSegments = 48;
        for (var index = 0; index < gradientSegments; index++)
        {
            var from = (float)index / gradientSegments;
            var to = (float)(index + 1) / gradientSegments;
            canvas.StrokeColor = GradientColorAt((from + to) / 2);
            DrawArcSegment(canvas, centerX, centerY, radius, from, to);
        }

        if (!HasEntry || Bmi <= 0)
            return;

        var markerFraction = (float)Fraction(Bmi);
        var angle = Math.PI * (1 - markerFraction);
        var markerX = centerX + radius * (float)Math.Cos(angle);
        var markerY = centerY - radius * (float)Math.Sin(angle);

        const float markerRadius = 7f;
        canvas.FillColor = MarkerFillColor;
        canvas.FillEllipse(markerX - markerRadius, markerY - markerRadius, markerRadius * 2, markerRadius * 2);
        canvas.StrokeColor = MarkerRingColor;
        canvas.StrokeSize = 3f;
        canvas.DrawEllipse(markerX - markerRadius, markerY - markerRadius, markerRadius * 2, markerRadius * 2);
    }

    private double Fraction(double bmi) => Math.Clamp((bmi - MinBmi) / (MaxBmi - MinBmi), 0, 1);

    private static Color GradientColorAt(float t) =>
        t <= 0.5f ? Lerp(GradientStart, GradientMid, t / 0.5f) : Lerp(GradientMid, GradientEnd, (t - 0.5f) / 0.5f);

    private static Color Lerp(Color a, Color b, float t) => new(
        a.Red + (b.Red - a.Red) * t,
        a.Green + (b.Green - a.Green) * t,
        a.Blue + (b.Blue - a.Blue) * t,
        a.Alpha + (b.Alpha - a.Alpha) * t);

    private static void DrawArcSegment(ICanvas canvas, float centerX, float centerY, float radius, float fromFraction, float toFraction)
    {
        const int segments = 6;
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
