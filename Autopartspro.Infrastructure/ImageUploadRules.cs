using Microsoft.AspNetCore.Http;

namespace Autopartspro.Infrastructure;

public static class ImageUploadRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif",
    };

    public static void Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("Choose an image file to upload.");

        if (file.Length > MaxBytes)
            throw new ArgumentException("Image must be 5 MB or smaller.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new ArgumentException("Use JPEG, PNG, WebP, or GIF.");
    }

    public static string GuessExtension(IFormFile file) =>
        file.ContentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg",
        };
}
