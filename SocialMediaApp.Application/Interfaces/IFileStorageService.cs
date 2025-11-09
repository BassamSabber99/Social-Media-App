namespace SocialMediaApp.Application.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to storage and returns its URL
    /// </summary>
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string bucket, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage by its URL
    /// </summary>
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a file stream for downloading
    /// </summary>
    Task<Stream> GetFileStreamAsync(string fileUrl, CancellationToken cancellationToken = default);
}

