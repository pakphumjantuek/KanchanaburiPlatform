using System.Globalization;
using System.Text;

namespace KanchanaburiPlatform.Application.Common.Export;

public sealed class CsvBuilder
{
    private readonly StringBuilder builder = new();

    public List<string[]> Rows { get; } = [];

    public void Row(params object?[] values)
    {
        var row = values
            .Select(v =>
                Convert.ToString(
                    v,
                    CultureInfo.InvariantCulture
                ) ?? string.Empty
            )
            .ToArray();

        Rows.Add(row);

        builder.AppendLine(
            string.Join(
                ',',
                row.Select(v =>
                    $"\"{v.Replace("\"", "\"\"")}\""
                )
            )
        );
    }

    public byte[] Bytes()
    {
        return Encoding.UTF8
            .GetPreamble()
            .Concat(
                Encoding.UTF8.GetBytes(
                    builder.ToString()
                )
            )
            .ToArray();
    }
}