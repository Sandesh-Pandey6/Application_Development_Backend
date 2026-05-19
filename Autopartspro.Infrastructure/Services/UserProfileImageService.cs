using Autopartspro.Application.Interfaces;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services;

public class UserProfileImageService : IUserProfileImageService
{
    private readonly AppDbContext _db;
    private readonly IImageStorageService _storage;

    public UserProfileImageService(AppDbContext db, IImageStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<(string Url, string? PublicId)> UploadAsync(
        Guid userId,
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        await _storage.DeleteAsync(user.ProfileImageUrl, user.ProfileImagePublicId, cancellationToken);

        var result = await _storage.UploadAsync(
            new ImageUploadRequest(stream, fileName, contentType, "profiles", userId.ToString("N")),
            cancellationToken);

        user.ProfileImageUrl = result.Url;
        user.ProfileImagePublicId = result.PublicId;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return (result.Url, result.PublicId);
    }

    public async Task RemoveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        await _storage.DeleteAsync(user.ProfileImageUrl, user.ProfileImagePublicId, cancellationToken);
        user.ProfileImageUrl = null;
        user.ProfileImagePublicId = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
