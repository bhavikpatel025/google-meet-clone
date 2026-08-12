using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VideoCallApp.Application.Interfaces;
using VideoCallApp.Infrastructure.Configuration;

namespace VideoCallApp.Infrastructure.Services;

public class PhotoService : IPhotoService
{
    private readonly Cloudinary _cloudinary;
    private readonly string _folderName;

    public PhotoService(IOptions<CloudinarySettings> config, IWebHostEnvironment environment)
    {
        _cloudinary = new Cloudinary(config.Value.Url);
        _cloudinary.Api.Secure = true;
        _folderName = environment.IsDevelopment() ? "G-meet_dev" : "G-meet_prod";
    }

    public async Task<string?> AddPhotoAsync(Stream fileStream, string fileName)
    {
        if (fileStream.Length == 0) return null;

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = _folderName
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new Exception(uploadResult.Error.Message);
        }

        return uploadResult.SecureUrl.AbsoluteUri;
    }

    public async Task<bool> DeletePhotoAsync(string publicUrl)
    {
        if (string.IsNullOrWhiteSpace(publicUrl)) return true;
        
        // Extract public ID from URL
        var segments = new Uri(publicUrl).Segments;
        var fileName = segments.Last();
        var publicId = Path.GetFileNameWithoutExtension(fileName);
        
        // Include folder name in public ID if applicable
        var fullPublicId = string.IsNullOrWhiteSpace(_folderName) ? publicId : $"{_folderName}/{publicId}";

        var deleteParams = new DeletionParams(fullPublicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);

        return result.Result == "ok";
    }
}
