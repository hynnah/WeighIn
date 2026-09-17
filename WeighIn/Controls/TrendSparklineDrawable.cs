namespace WeighIn.Controls;

public sealed class TrendSparklineDrawable : IDrawable
{
    public List<double> Values { get; set; } = [];
    public List<double> MovingAverage { get; set; } = [];

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Values.Count < 2)
            return;

        var min = Math.Min(Values.Min(), MovingAverage.Count > 0 ? MovingAverage.Min() : double.MaxValue);
        var max = Math.Max(Values.Max(), MovingAverage.Count > 0 ? MovingAverage.Max() : double.MinValue);

        if (MovingAverage.Count == Values.Count)
            DrawLine(canvas, MovingAverage, dirtyRect, min, max, Color.FromArgb("#75798C"), 1.5f);

        DrawLine(canvas, Values, dirtyRect, min, max, Color.FromArgb("#9184D9"), 2f);
    }

    private static void DrawLine(ICanvas canvas, List<double> values, RectF rect, double min, double max, Color color, float strokeSize)
    {
        var range = max - min;
        if (range < 0.001)
            range = 1;

        canvas.StrokeColor = color;
        canvas.StrokeSize = strokeSize;
        canvas.StrokeLineCap = LineCap.Round;

        const float verticalPadding = 0.12f;
        var points = new PointF[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            var x = values.Count == 1 ? 0 : rect.Width * index / (values.Count - 1);
            var normalized = (values[index] - min) / range;
            var y = rect.Height * (1 - verticalPadding * 2) * (1 - (float)normalized) + rect.Height * verticalPadding;
            points[index] = new PointF(x, y);
        }

        for (var index = 1; index < points.Length; index++)
            canvas.DrawLine(points[index - 1], points[index]);
    }
}
