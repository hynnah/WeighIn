namespace WeighIn.Controls;

public sealed class ContributionGridDrawable : IDrawable
{
    private const int Weeks = 26;
    private const int Days = 7;

    public HashSet<DateTime> LoggedDays { get; set; } = [];
    public Color MissedColor { get; set; } = Color.FromArgb("#232532");
    public Color FutureColor { get; set; } = Color.FromRgba(35, 37, 50, 89);

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float cellGap = 2f;
        var cellSize = Math.Min(
            (dirtyRect.Width - cellGap * (Weeks - 1)) / Weeks,
            (dirtyRect.Height - cellGap * (Days - 1)) / Days);

        var today = DateTime.Today;
        var startDate = today.AddDays(-(Weeks * Days - 1));

        for (var week = 0; week < Weeks; week++)
        {
            for (var day = 0; day < Days; day++)
            {
                var date = startDate.AddDays(week * Days + day);
                var x = dirtyRect.X + week * (cellSize + cellGap);
                var y = dirtyRect.Y + day * (cellSize + cellGap);

                canvas.FillColor = date > today
                    ? FutureColor
                    : LoggedDays.Contains(date.Date)
                        ? Color.FromArgb("#8878D6")
                        : MissedColor;

                canvas.FillRoundedRectangle(x, y, cellSize, cellSize, 2);
            }
        }
    }
}
