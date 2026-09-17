namespace WeighIn.Controls;

public sealed class BmiGaugeDrawable : IDrawable
{
    public double Bmi { get; set; }
    public double MinBmi { get; set; } = 15;
    public double MaxBmi { get; set; } = 35;
    public Color TrackColor { get; set; } = Color.FromArgb("#232532");
    public Color FillColor { get; set; } = Color.FromArgb("#9184D9");

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

        canvas.StrokeColor = TrackColor;
        DrawSemicircleSegment(canvas, centerX, centerY, radius, 0, 1);

        if (Bmi <= 0)
            return;

        var fraction = (float)Math.Clamp((Bmi - MinBmi) / (MaxBmi - MinBmi), 0.02, 1);
        canvas.StrokeColor = FillColor;
        DrawSemicircleSegment(canvas, centerX, centerY, radius, 0, fraction);
    }

    private static void DrawSemicircleSegment(ICanvas canvas, float centerX, float centerY, float radius, float fromFraction, float toFraction)
    {
        const int segments = 40;
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
