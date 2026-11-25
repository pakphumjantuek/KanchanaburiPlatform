using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace KanchanaburiPlatform.Application.Common.Export;

public static class ExportPdf
{
    public static byte[] Create(
        string title,
        IReadOnlyList<string[]> rows
    )
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);

                page.Header()
                    .Text(title)
                    .FontSize(18)
                    .Bold();

                page.Content()
                    .PaddingVertical(15)
                    .Table(table =>
                    {
                        if (rows.Count == 0)
                            return;

                        var columnCount = rows[0].Length;

                        table.ColumnsDefinition(columns =>
                        {
                            for (int i = 0; i < columnCount; i++)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                        {
                            var row = rows[rowIndex];

                            foreach (var value in row)
                            {
                                var cell = table.Cell()
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5);

                                if (rowIndex == 0)
                                {
                                    cell.Text(value)
                                        .Bold();
                                }
                                else
                                {
                                    cell.Text(value);
                                }
                            }
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
            });
        })
        .GeneratePdf();
    }
}