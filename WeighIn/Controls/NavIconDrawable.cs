namespace WeighIn.Controls;

public enum NavIconKind
{
    Home,
    Trends,
    Add,
    Calendar,
    More,
    Sliders
}

public sealed class NavIconDrawable : IDrawable
{
    private const float IconSize = 22f;

    public NavIconKind Kind { get; set; }
    public Color Color { get; set; } = Colors.Gray;

    public void Draw(ICanvas canvas, RectF rect)
    {
        var cx = rect.Center.X;
        var cy = rect.Center.Y;
        var half = IconSize * 0.34f;

        canvas.StrokeColor = Color;
        canvas.FillColor = Color;
        canvas.StrokeSize = Kind == NavIconKind.Add ? 2.2f : 1.6f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        switch (Kind)
        {
            case NavIconKind.Home:
                DrawHome(canvas, cx, cy, half);
                break;
            case NavIconKind.Trends:
                DrawTrends(canvas, cx, cy, half);
                break;
            case NavIconKind.Add:
                DrawAdd(canvas, cx, cy, half);
                break;
            case NavIconKind.Calendar:
                DrawCalendar(canvas, cx, cy, half);
                break;
            case NavIconKind.More:
                DrawMore(canvas, cx, cy, half);
                break;
            case NavIconKind.Sliders:
                DrawSliders(canvas, cx, cy, half);
                break;
        }
    }

    private static void DrawHome(ICanvas canvas, float cx, float cy, float half)
    {
        var top = cy - half;
        var bottom = cy + half * 0.75f;
        var left = cx - half * 0.9f;
        var right = cx + half * 0.9f;

        var roof = new PathF();
        roof.MoveTo(left, cy - half * 0.1f);
        roof.LineTo(cx, top);
        roof.LineTo(right, cy - half * 0.1f);
        canvas.DrawPath(roof);

        var body = new PathF();
        body.MoveTo(left + 1.5f, cy - half * 0.1f);
        body.LineTo(left + 1.5f, bottom);
        body.LineTo(right - 1.5f, bottom);
        body.LineTo(right - 1.5f, cy - half * 0.1f);
        canvas.DrawPath(body);
    }

    private static void DrawTrends(ICanvas canvas, float cx, float cy, float half)
    {
        var points = new[]
        {
            new PointF(cx - half, cy + half * 0.5f),
            new PointF(cx - half * 0.25f, cy - half * 0.1f),
            new PointF(cx + half * 0.2f, cy + half * 0.2f),
            new PointF(cx + half, cy - half * 0.8f)
        };
        for (var index = 1; index < points.Length; index++)
            canvas.DrawLine(points[index - 1], points[index]);

        var tip = points[^1];
        canvas.DrawLine(tip.X - half * 0.5f, tip.Y, tip.X, tip.Y);
        canvas.DrawLine(tip.X, tip.Y, tip.X, tip.Y + half * 0.5f);
    }

    private static void DrawAdd(ICanvas canvas, float cx, float cy, float half)
    {
        canvas.DrawLine(cx - half, cy, cx + half, cy);
        canvas.DrawLine(cx, cy - half, cx, cy + half);
    }

    private static void DrawCalendar(ICanvas canvas, float cx, float cy, float half)
    {
        var width = half * 1.8f;
        var height = half * 1.6f;
        var left = cx - width / 2;
        var top = cy - height / 2 + half * 0.15f;

        canvas.DrawRoundedRectangle(left, top, width, height, 3);
        canvas.DrawLine(left, top + height * 0.32f, left + width, top + height * 0.32f);
        canvas.DrawLine(cx - width * 0.22f, top - half * 0.28f, cx - width * 0.22f, top + height * 0.1f);
        canvas.DrawLine(cx + width * 0.22f, top - half * 0.28f, cx + width * 0.22f, top + height * 0.1f);
    }

    private static void DrawMore(ICanvas canvas, float cx, float cy, float half)
    {
        var gap = half * 0.85f;
        var radius = half * 0.16f;
        for (var index = -1; index <= 1; index++)
            canvas.FillCircle(cx + index * gap, cy, radius);
    }

    private static void DrawSliders(ICanvas canvas, float cx, float cy, float half)
    {
        var lineLength = half * 1.7f;
        var left = cx - lineLength / 2;
        var right = cx + lineLength / 2;
        float[] rowY = { cy - half * 0.6f, cy, cy + half * 0.6f };
        float[] handleX = { left + lineLength * 0.32f, left + lineLength * 0.68f, left + lineLength * 0.5f };
        var handleRadius = half * 0.15f;

        for (var index = 0; index < rowY.Length; index++)
        {
            canvas.DrawLine(left, rowY[index], right, rowY[index]);
            canvas.FillCircle(handleX[index], rowY[index], handleRadius);
        }
    }
}

public static class NavBarColors
{
    public static Color Active => Color.FromArgb("#9184D9");
    public static Color Muted(bool isDark) => isDark ? Color.FromArgb("#75798C") : Color.FromArgb("#9C9990");
}
