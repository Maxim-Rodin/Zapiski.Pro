using System;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using CloudinaryDotNet.Actions;

namespace Zapiski.Pro.ClassMiniApp.Services;

public class CloudinaryImageService //класс сервис для работы с фотографиями добалвания и хранения их в облаке 
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new() { "image/jpeg", "image/png", "image/webp" };//какие форматы поддерживает
    private readonly Cloudinary cloudinary;

    public CloudinaryImageService()
    {
        var cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
        var apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
        var apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");
        
        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException("Непрвильные данные подключение к базе фото");
        }
        var account = new Account(cloudName, apiKey, apiSecret);
        cloudinary = new Cloudinary(account);
        cloudinary.Api.Secure = true;
    }

    public async Task<string> UploadMasterAvatar(int masterId, IFormFile file)
    {
        ValidateImage(file);
        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = $"zapiski/masters/{masterId}",
            PublicId = "avatar",
            Overwrite = true,
            Transformation = new Transformation()
                .Width(800)
                .Height(800)
                .Crop("fill")
                .Gravity("face")
                .FetchFormat("auto")
                .Quality("auto")
        };
        var result = await cloudinary.UploadAsync(uploadParams);
        if (result.Error != null)
        {
            throw new InvalidOperationException(result.Error.Message);
        }
        return result.SecureUrl.ToString();
    }
    private static void ValidateImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("Файл не выбран");

        if (file.Length > MaxFileSize)
            throw new InvalidOperationException("Файл слишком большой. Максимум 5 MB");

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Можно загрузить только JPG, PNG или WEBP");
    }
}