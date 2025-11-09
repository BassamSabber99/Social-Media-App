using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using SocialMediaApp.Application.Configuration;
using SocialMediaApp.Application.Interfaces;

namespace SocialMediaApp.Infrastructure.Services;

public class MinIOFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly FileStorageOptions _options;

    public MinIOFileStorageService(IMinioClient minioClient, IOptions<FileStorageOptions> options)
    {
        _minioClient = minioClient;
        _options = options.Value;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string bucket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);

        try
        {
            // Ensure bucket exists
            await EnsureBucketExistsAsync(bucket, cancellationToken);

            // Generate unique file name to prevent collisions
            var uniqueFileName = $"{Guid.NewGuid()}/{SanitizeFileName(fileName)}";

            // Upload file with streaming (memory-efficient, no buffering)
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(uniqueFileName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            // Generate the file URL
            var endpoint = _options.MinIO.UseSSL ? "https" : "http";
            var fileUrl = $"{endpoint}://{_options.MinIO.Endpoint}/{bucket}/{uniqueFileName}";

            return fileUrl;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to upload file to MinIO: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileUrl);

        try
        {
            var (bucket, objectName) = ParseFileUrl(fileUrl);

            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete file from MinIO: {ex.Message}", ex);
        }
    }

    public async Task<Stream> GetFileStreamAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileUrl);

        try
        {
            var (bucket, objectName) = ParseFileUrl(fileUrl);
            var memoryStream = new MemoryStream();

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithCallbackStream(async (stream) =>
                {
                    await stream.CopyToAsync(memoryStream, cancellationToken);
                    memoryStream.Position = 0;
                });

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

            return memoryStream;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve file from MinIO: {ex.Message}", ex);
        }
    }

    private async Task EnsureBucketExistsAsync(string bucket, CancellationToken cancellationToken)
    {
        var bucketExistsArgs = new BucketExistsArgs()
            .WithBucket(bucket);

        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

        if (!exists)
        {
            var makeBucketArgs = new MakeBucketArgs()
                .WithBucket(bucket);

            await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);

            // Set bucket policy to public read (for file access)
            var policy = GeneratePublicReadPolicy(bucket);
            var setBucketPolicyArgs = new SetPolicyArgs()
                .WithBucket(bucket)
                .WithPolicy(policy);

            await _minioClient.SetPolicyAsync(setBucketPolicyArgs, cancellationToken);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove any path characters and keep only the file name
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized;
    }

    private (string bucket, string objectName) ParseFileUrl(string fileUrl)
    {
        var uri = new Uri(fileUrl);
        var segments = uri.AbsolutePath.TrimStart('/').Split('/', 2);
        
        if (segments.Length < 2)
        {
            throw new ArgumentException("Invalid file URL format", nameof(fileUrl));
        }

        return (segments[0], segments[1]);
    }

    private static string GeneratePublicReadPolicy(string bucketName)
    {
        return $$"""
        {
          "Version": "2012-10-17",
          "Statement": [
            {
              "Effect": "Allow",
              "Principal": {"AWS": "*"},
              "Action": ["s3:GetObject"],
              "Resource": ["arn:aws:s3:::{{bucketName}}/*"]
            }
          ]
        }
        """;
    }
}

