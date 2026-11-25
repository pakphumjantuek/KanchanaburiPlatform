using Microsoft.AspNetCore.Http;

namespace Application.DTOs;

public sealed class UploadPaymentSlipFormDto
{
    public IFormFile? SlipFile { get; set; }
    public DateTime? TransferTime { get; set; }
}
