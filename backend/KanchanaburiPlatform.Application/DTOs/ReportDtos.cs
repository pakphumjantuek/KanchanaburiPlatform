namespace Application.DTOs;

public sealed class CreateReportDto
{
    public Guid? ContentId { get; set; }
    public Guid? ReviewId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class UpdateReportStatusDto
{
    public string Status { get; set; } = "Pending";
}

public sealed class ReportDto
{
    public Guid ReportId { get; set; }
    public Guid? ContentId { get; set; }
    public Guid? ReviewId { get; set; }
    public string TargetLabel { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
