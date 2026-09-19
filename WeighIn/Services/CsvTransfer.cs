using System.Globalization;
using System.Text;
using WeighIn.Models;

namespace WeighIn.Services;

public sealed record CsvImportResult(List<WeightEntry> Entries, int SkippedCount);

public static class CsvTransfer
{
    private const string Header = "Date,Time,WeightKg,HeightCm,Note";
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm";

    public static string Export(IEnumerable<WeightEntry> entries)
    {
        var builder = new StringBuilder();
        builder.Append(Header).Append('\n');

        foreach (var entry in entries.OrderBy(entry => entry.DateTime))
        {
            builder.Append(entry.DateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(entry.DateTime.ToString("HH:mm", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(entry.WeightKg.ToString("0.##", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(entry.HeightCmAtEntry.ToString("0.##", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(EscapeField(entry.Note ?? string.Empty)).Append('\n');
        }

        return builder.ToString();
    }

    public static CsvImportResult Import(string csvText, double fallbackHeightCm)
    {
        var lines = csvText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var entries = new List<WeightEntry>();
        var skipped = 0;

        foreach (var line in lines.Skip(1))
        {
            var fields = ParseLine(line);
            if (fields.Count < 3)
            {
                skipped++;
                continue;
            }

            if (!DateTime.TryParseExact($"{fields[0]} {fields[1]}", DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime)
                || !double.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var weightKg)
                || weightKg is < 20 or > 400)
            {
                skipped++;
                continue;
            }

            var heightCm = fields.Count > 3 && double.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedHeight) && parsedHeight > 0
                ? parsedHeight
                : fallbackHeightCm;
            var note = fields.Count > 4 ? fields[4] : null;

            entries.Add(new WeightEntry
            {
                DateTime = dateTime,
                WeightKg = weightKg,
                HeightCmAtEntry = heightCm,
                Note = string.IsNullOrWhiteSpace(note) ? null : note
            });
        }

        return new CsvImportResult(entries, skipped);
    }

    private static string EscapeField(string value)
    {
        if (value.IndexOfAny([',', '"', '\n']) < 0)
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static List<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var c = line[index];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (index + 1 < line.Length && line[index + 1] == '"')
                    {
                        current.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
