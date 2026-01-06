namespace Tarot.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string[] allowedExtensions, long maxSizeBytes = 5242880);
    Task DeleteFileAsync(string fileUrl);
}