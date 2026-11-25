using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.Services;

public static class ExportPdf
{
    public static byte[] Create(string title, IReadOnlyList<string[]> rows)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var headers = rows.FirstOrDefault() ?? [];
        var data = rows.Skip(1).ToList();
        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape()); page.Margin(20);
            page.DefaultTextStyle(x => x.FontFamily("Tahoma").FontSize(8));
            page.Header().Column(column => { column.Item().Text(title).FontSize(16).Bold(); column.Item().Text($"สร้างเมื่อ {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC").FontSize(8).FontColor(Colors.Grey.Darken1); });
            page.Content().PaddingVertical(10).Table(table => { table.ColumnsDefinition(columns => { foreach (var _ in headers) columns.RelativeColumn(); }); table.Header(header => { foreach (var value in headers) header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text(value).FontColor(Colors.White).Bold(); }); foreach (var row in data) foreach (var value in row) table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(value ?? string.Empty); });
            page.Footer().AlignCenter().Text(text => { text.Span("หน้า "); text.CurrentPageNumber(); text.Span(" / "); text.TotalPages(); });
        })).GeneratePdf();
    }
}
