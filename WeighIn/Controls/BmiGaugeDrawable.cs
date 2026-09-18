namespace WeighIn.Controls;

public sealed class BmiGaugeDrawable : IDrawable
{
    // Offsets are (bmi - 16) / 16 for each BMI threshold in the gauge's 16-32 display window:
    // 18.5 -> 0.16 (enters Normal), 23 -> 0.44 (leaves Normal), 25 -> 0.56 (Overweight).
    private static readonly (float Offset, Color Dark, Color Light)[] GradientStops =
    [
        (0.00f, Color.FromArgb("#3C6EA8"), Color.FromArgb("#2F5C8F")),
        (0.16f, Color.FromArgb("#4A8F80"), Color.FromArgb("#37786A")),
        (0.30f, Color.FromArgb("#4F9E5C"), Color.FromArgb("#3A8347")),
        (0.44f, Color.FromArgb("#7AA84E"), Color.FromArgb("#688F3C")),
        (0.56f, Color.FromArgb("#D4B04A"), Color.FromArgb("#B8912E")),
        (0.72f, Color.FromArgb("#C8793C"), Color.FromArgb("#AB6528")),
        (1.00f, Color.FromArgb("#B8412C"), Color.FromArgb("#A6331F"))
    ];

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

        var trackColor = IsDark ? Color.FromArgb("#292B31") : Color.FromArgb("#E4E0D8");
        canvas.StrokeColor = trackColor;
        DrawArcSegment(canvas, centerX, centerY, radius, 0, 1);

        DrawGradientArc(canvas, centerX, centerY, radius, strokeWidth);

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

    private void DrawGradientArc(ICanvas canvas, float centerX, float centerY, float radius, float strokeWidth)
    {
        var outerRadius = radius + strokeWidth / 2;
        var innerRadius = radius - strokeWidth / 2;
        var capRadius = strokeWidth / 2;

        const int arcSegments = 64;
        var path = new PathF();

        for (var index = 0; index <= arcSegments; index++)
        {
            var t = (float)index / arcSegments;
            var pointAngle = Math.PI * (1 - t);
            var x = centerX + outerRadius * (float)Math.Cos(pointAngle);
            var y = centerY - outerRadius * (float)Math.Sin(pointAngle);
            if (index == 0)
                path.MoveTo(x, y);
            else
                path.LineTo(x, y);
        }

        for (var index = arcSegments; index >= 0; index--)
        {
            var t = (float)index / arcSegments;
            var pointAngle = Math.PI * (1 - t);
            var x = centerX + innerRadius * (float)Math.Cos(pointAngle);
            var y = centerY - innerRadius * (float)Math.Sin(pointAngle);
            path.LineTo(x, y);
        }

        path.Close();

        var gradientStops = new PaintGradientStop[GradientStops.Length];
        for (var index = 0; index < GradientStops.Length; index++)
        {
            var (offset, dark, light) = GradientStops[index];
            gradientStops[index] = new PaintGradientStop(offset, IsDark ? dark : light);
        }

        var paint = new LinearGradientPaint
        {
            StartPoint = new PointF(0, 0.5f),
            EndPoint = new PointF(1, 0.5f),
            GradientStops = gradientStops
        };

        var bounds = new RectF(centerX - outerRadius, centerY - outerRadius, outerRadius * 2, outerRadius);
        canvas.SetFillPaint(paint, bounds);

        canvas.FillPath(path);

        var leftX = centerX - radius;
        var rightX = centerX + radius;
        canvas.FillEllipse(leftX - capRadius, centerY - capRadius, capRadius * 2, capRadius * 2);
        canvas.FillEllipse(rightX - capRadius, centerY - capRadius, capRadius * 2, capRadius * 2);
    }

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
