namespace SocialMediaApp.Application.Configuration;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public int MaxFileSizeMB { get; set; } = 10;
    public int MaxVoiceNoteSizeMB { get; set; } = 5;
    public string[] AllowedFileTypes { get; set; } = Array.Empty<string>();
    public MinIOOptions MinIO { get; set; } = new();

    public long MaxFileSizeBytes => MaxFileSizeMB * 1024 * 1024;
    public long MaxVoiceNoteSizeBytes => MaxVoiceNoteSizeMB * 1024 * 1024;
}

public class MinIOOptions
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "chat-files";
    public bool UseSSL { get; set; } = false;
}

