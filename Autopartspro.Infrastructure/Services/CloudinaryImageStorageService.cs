using Autopartspro.Application.Interfaces;
using Autopartspro.Application.Options;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace Autopartspro.Infrastructure.Services;

public class CloudinaryImageStorageService : IImageStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly string _baseFolder;

    public CloudinaryImageStorageService(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;
        var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
        _cloudinary = new Cloudinary(account);
        _baseFolder = settings.BaseFolder.Trim().Trim('/');
    }

    public async Task<StoredImageResult> UploadAsync(ImageUploadRequest request, CancellationToken cancellationToken = default)
    {
        var folder = CombineFolder(request.Folder);
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(request.FileName, request.Stream),
            Folder = folder,
            Overwrite = true,
        };

        if (!string.IsNullOrWhiteSpace(request.PublicId))
            uploadParams.PublicId = request.PublicId.Trim('/');

        var result = await _cloudinary.UploadAsync(uploadParams);
        if (result.Error != null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

        return new StoredImageResult(result.SecureUrl.ToString(), result.PublicId);
    }

    public Task DeleteAsync(string? imageUrl, string? publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return Task.CompletedTask;

        var resolvedId = publicId.Contains('/')
            ? publicId
            : publicId;

        return _cloudinary.DestroyAsync(new DeletionParams(resolvedId)
        {
            ResourceType = ResourceType.Image,
        });
    }

    private string CombineFolder(string folder)
    {
        var sub = folder.Trim().Trim('/');
        return string.IsNullOrEmpty(sub) ? _baseFolder : $"{_baseFolder}/{sub}";
    }
}
