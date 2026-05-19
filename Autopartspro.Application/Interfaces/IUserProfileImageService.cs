namespace Autopartspro.Application.Interfaces;

public interface IUserProfileImageService
{
    Task<(string Url, string? PublicId)> UploadAsync(
        Guid userId,
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid userId, CancellationToken cancellationToken = default);
}
