namespace Autopartspro.Application.Interfaces;

public record StoredImageResult(string Url, string? PublicId);

public record ImageUploadRequest(
    Stream Stream,
    string FileName,
    string ContentType,
    string Folder,
    string? PublicId = null);

public interface IImageStorageService
{
    Task<StoredImageResult> UploadAsync(ImageUploadRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? imageUrl, string? publicId, CancellationToken cancellationToken = default);
}
