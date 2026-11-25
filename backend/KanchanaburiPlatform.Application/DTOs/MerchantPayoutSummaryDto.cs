namespace Application.DTOs;

using Microsoft.AspNetCore.Http;

public sealed class AdminConfirmPayoutFormDto
{
    public Guid ShopId { get; set; }
    public string OrderIds { get; set; } = string.Empty;
    public string? TransactionRef { get; set; }
    public string? Note { get; set; }
    public IFormFile? SlipFile { get; set; }
}

public sealed class MerchantPayoutSummaryDto
{
    public decimal PendingPayoutAmount { get; set; }
    public decimal TotalPaidOutAmount { get; set; }
    public List<MerchantPayoutRecordDto> History { get; set; } = [];
}
