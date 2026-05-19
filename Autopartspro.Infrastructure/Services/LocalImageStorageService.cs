using Autopartspro.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace Autopartspro.Infrastructure.Services;

/// <summary>Fallback when Cloudinary is not configured (development).</summary>
public class LocalImageStorageService : IImageStorageService
{
    private readonly string _webRoot;

    public LocalImageStorageService(IWebHostEnvironment env)
    {
        _webRoot = string.IsNullOrWhiteSpace(env.WebRootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot")
            : env.WebRootPath;
    }

    public async Task<StoredImageResult> UploadAsync(ImageUploadRequest request, CancellationToken cancellationToken = default)
    {
        var relativeFolder = request.Folder.Trim().Trim('/');
        var physicalDir = Path.Combine(_webRoot, "uploads", relativeFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(physicalDir);

        var ext = Path.GetExtension(request.FileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 6)
            ext = ".jpg";

        var fileName = string.IsNullOrWhiteSpace(request.PublicId)
            ? $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}"
            : $"{request.PublicId.Trim('/')}{ext.ToLowerInvariant()}";

        var physicalPath = Path.Combine(physicalDir, fileName);
        await using (var fs = new FileStream(physicalPath, FileMode.Create))
        {
            await request.Stream.CopyToAsync(fs, cancellationToken);
        }

        var url = $"/uploads/{relativeFolder}/{fileName}".Replace('\\', '/');
        return new StoredImageResult(url, null);
    }

    public Task DeleteAsync(string? imageUrl, string? publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var relative = imageUrl.TrimStart('/');
        if (!relative.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var path = Path.Combine(_webRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path))
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }

        return Task.CompletedTask;
    }
}
