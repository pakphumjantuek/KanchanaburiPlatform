using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public sealed record StoredImage(string Url, string PhysicalPath);

public interface IImageUploadService
{
    Task<StoredImage?> SaveAsync(IFormFile file, string uploadFolder, CancellationToken cancellationToken = default);
    void Delete(StoredImage image);
}

public sealed class ImageUploadService(IWebHostEnvironment environment) : IImageUploadService
{
    private const long MaxImageSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
    };

    public async Task<StoredImage?> SaveAsync(IFormFile file, string uploadFolder, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName);

        if (file.Length is <= 0 or > MaxImageSize || !AllowedExtensions.Contains(extension))
        {
            return null;
        }

        if (!await HasValidImageSignatureAsync(file, extension, cancellationToken))
        {
            return null;
        }

        var folder = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", uploadFolder.Trim('/').Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(folder, fileName);
        await using var stream = File.Create(physicalPath);
        await file.CopyToAsync(stream, cancellationToken);
        return new($"/uploads/{uploadFolder.Trim('/')}/{fileName}", physicalPath);
    }

    public void Delete(StoredImage image)
    {
        if (File.Exists(image.PhysicalPath))
        {
            File.Delete(image.PhysicalPath);
        }
    }

    private static async Task<bool> HasValidImageSignatureAsync(
        IFormFile file,
        string extension,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];

        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header, cancellationToken);

        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => bytesRead >= 3
                && header[0] == 0xFF
                && header[1] == 0xD8
                && header[2] == 0xFF,
            ".png" => bytesRead >= 8
                && header.AsSpan(0, 8).SequenceEqual(
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
                ),
            ".webp" => bytesRead >= 12
                && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false,
        };
    }
}
