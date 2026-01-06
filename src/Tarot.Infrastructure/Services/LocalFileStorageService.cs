using Microsoft.AspNetCore.Hosting;
using Tarot.Core.Interfaces;

namespace Tarot.Infrastructure.Services;

public class LocalFileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    private readonly IWebHostEnvironment _env = env;
    private const string UploadFolder = "uploads";

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string[] allowedExtensions, long maxSizeBytes = 5242880)
    {
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("File is empty");

        if (fileStream.Length > maxSizeBytes)
            throw new ArgumentException($"File size exceeds limit of {maxSizeBytes / 1024 / 1024}MB");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            throw new ArgumentException("Invalid file type");

        // Basic magic number check for images
        if (IsImageExtension(ext))
        {
             if (!IsValidImageHeader(fileStream))
                throw new ArgumentException("Invalid image file content");
        }

        var newFileName = $"{Guid.NewGuid()}{ext}";
        var uploadPath = Path.Combine(_env.WebRootPath, UploadFolder);
        
        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        var filePath = Path.Combine(uploadPath, newFileName);
        
        // Reset stream position if needed
        if (fileStream.CanSeek) fileStream.Position = 0;
        
        using (var destStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(destStream);
        }

        return $"/{UploadFolder}/{newFileName}";
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return Task.CompletedTask;

        var fileName = Path.GetFileName(fileUrl);
        var filePath = Path.Combine(_env.WebRootPath, UploadFolder, fileName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    private bool IsImageExtension(string ext)
    {
        return new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" }.Contains(ext);
    }

    private bool IsValidImageHeader(Stream stream)
    {
        try
        {
            if (stream.CanSeek) stream.Position = 0;
            
            var header = new byte[4];
            if (stream.Read(header, 0, 4) < 4) return false;
            
            var hex = BitConverter.ToString(header).Replace("-", "");
            
            // JPG: FF D8 FF
            if (hex.StartsWith("FFD8FF")) return true; 
            // PNG: 89 50 4E 47
            if (hex.StartsWith("89504E47")) return true; 
            // GIF: 47 49 46 38
            if (hex.StartsWith("47494638")) return true; 
            // BMP: 42 4D
            if (hex.StartsWith("424D")) return true; 
            // WEBP: RIFF....WEBP (Requires reading more bytes, simplified check: starts with RIFF)
            if (hex.StartsWith("52494646")) return true; 
            
            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
             if (stream.CanSeek) stream.Position = 0;
        }
    }
}