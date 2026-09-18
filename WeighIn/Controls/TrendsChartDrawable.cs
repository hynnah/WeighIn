namespace WeighIn.Controls;

public sealed class TrendsChartDrawable : IDrawable
{
    public List<double> Values { get; set; } = [];
    public List<double> MovingAverage { get; set; } = [];
    public double? GoalValue { get; set; }
    public int SelectedIndex { get; set; } = -1;
    public bool IsDark { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Values.Count == 0)
            return;

        const float padding = 10f;
        var width = dirtyRect.Width;
        var height = dirtyRect.Height;

        var all = new List<double>(Values);
        if (MovingAverage.Count == Values.Count)
            all.AddRange(MovingAverage);
        if (GoalValue.HasValue)
            all.Add(GoalValue.Value);

        var lo = all.Min();
        var hi = all.Max();
        if (hi - lo < 0.4)
        {
            hi += 0.2;
            lo -= 0.2;
        }

        var span = hi - lo;

        float X(int index) => Values.Count < 2 ? width / 2 : width * index / (Values.Count - 1);
        float Y(double value) => padding + (float)((hi - value) / span) * (height - padding * 2);

        var gridColor = IsDark ? Color.FromRgba(233, 233, 237, 20) : Color.FromRgba(24, 44, 43, 20);
        canvas.StrokeColor = gridColor;
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = null;
        for (var line = 0; line < 4; line++)
        {
            var y = padding + (height - padding * 2) * line / 3f;
            canvas.DrawLine(0, y, width, y);
        }

        if (GoalValue.HasValue)
        {
            var goalY = Y(GoalValue.Value);
            canvas.StrokeColor = Color.FromArgb("#5D5294");
            canvas.StrokeSize = 1;
            canvas.StrokeDashPattern = [5, 4];
            canvas.DrawLine(0, goalY, width, goalY);
            canvas.StrokeDashPattern = null;
        }

        if (Values.Count > 1)
        {
            var areaPath = new PathF();
            areaPath.MoveTo(X(0), Y(Values[0]));
            for (var index = 1; index < Values.Count; index++)
                areaPath.LineTo(X(index), Y(Values[index]));
            areaPath.LineTo(width, height);
            areaPath.LineTo(0, height);
            areaPath.Close();

            var areaPaint = new LinearGradientPaint
            {
                StartPoint = new PointF(0, 0),
                EndPoint = new PointF(0, 1),
                GradientStops =
                [
                    new PaintGradientStop(0, Color.FromArgb("#9184D9").WithAlpha(0.28f)),
                    new PaintGradientStop(1, Color.FromArgb("#9184D9").WithAlpha(0f))
                ]
            };
            canvas.SetFillPaint(areaPaint, new RectF(0, 0, width, height));
            canvas.FillPath(areaPath);
        }

        if (MovingAverage.Count == Values.Count && Values.Count > 1)
        {
            canvas.StrokeColor = IsDark ? Color.FromRgba(233, 233, 237, 77) : Color.FromRgba(24, 44, 43, 77);
            canvas.StrokeSize = 1.5f;
            canvas.StrokeDashPattern = [4, 3];
            DrawPolyline(canvas, MovingAverage, X, Y);
            canvas.StrokeDashPattern = null;
        }

        if (Values.Count > 1)
        {
            canvas.StrokeColor = Color.FromArgb("#B5ABFC");
            canvas.StrokeSize = 2;
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.StrokeLineCap = LineCap.Round;
            DrawPolyline(canvas, Values, X, Y);
        }

        var selectedIndex = SelectedIndex < 0 || SelectedIndex >= Values.Count ? Values.Count - 1 : SelectedIndex;
        var selX = X(selectedIndex);
        var selY = Y(Values[selectedIndex]);

        canvas.StrokeColor = IsDark ? Color.FromRgba(233, 233, 237, 64) : Color.FromRgba(24, 44, 43, 64);
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = [3, 3];
        canvas.DrawLine(selX, 0, selX, height);
        canvas.StrokeDashPattern = null;

        const float markerRadius = 5f;
        canvas.FillColor = IsDark ? Color.FromArgb("#1D1F2E") : Color.FromArgb("#FFFFFF");
        canvas.FillEllipse(selX - markerRadius, selY - markerRadius, markerRadius * 2, markerRadius * 2);
        canvas.StrokeColor = IsDark ? Color.FromArgb("#E9E9ED") : Color.FromArgb("#182C2B");
        canvas.StrokeSize = 2.5f;
        canvas.DrawEllipse(selX - markerRadius, selY - markerRadius, markerRadius * 2, markerRadius * 2);
    }

    private static void DrawPolyline(ICanvas canvas, List<double> values, Func<int, float> x, Func<double, float> y)
    {
        var path = new PathF();
        path.MoveTo(x(0), y(values[0]));
        for (var index = 1; index < values.Count; index++)
            path.LineTo(x(index), y(values[index]));
        canvas.DrawPath(path);
    }
}
