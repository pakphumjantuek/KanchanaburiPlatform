using Application.Results;
using Core.Interfaces;

namespace KanchanaburiPlatform.Application.Common.Export;

public static class ExportHelper
{
    public static ServiceResult<ExportFileResult> Success(
        CsvBuilder csv,
        string name,
        bool pdf,
        Func<IReadOnlyList<string[]>, byte[]>? createPdf = null
    )
    {
        if (pdf && createPdf is not null)
        {
            return ServiceResult<ExportFileResult>.Success(
                new ExportFileResult(
                    createPdf(csv.Rows),
                    "application/pdf",
                    $"{name}.pdf"
                )
            );
        }

        return ServiceResult<ExportFileResult>.Success(
            new ExportFileResult(
                csv.Bytes(),
                "text/csv; charset=utf-8",
                $"{name}.csv"
            )
        );
    }

    public static ServiceResult<ExportFileResult> Invalid(
        string error
    )
    {
        return ServiceResult<ExportFileResult>.Failure(
            ServiceStatus.Invalid,
            error
        );
    }
}
