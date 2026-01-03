using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace GhanaHybridRentalApi.Services;

public interface IFileUploadService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<List<string>> UploadMultipleFilesAsync(List<(Stream stream, string fileName, string contentType)> files);
    Task<bool> DeleteFileAsync(string fileUrl);
    string GetFileUrl(string fileName);
}

public class LocalFileUploadService : IFileUploadService
{
    private readonly string _uploadPath;
    private readonly string _baseUrl;
    private readonly ILogger<LocalFileUploadService> _logger;

    public LocalFileUploadService(IConfiguration configuration, ILogger<LocalFileUploadService> logger)
    {
        _uploadPath = configuration["FileUpload:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _baseUrl = configuration["FileUpload:BaseUrl"] ?? "/uploads";
        _logger = logger;

        // Ensure upload directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            // Generate unique filename
            var extension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadPath, uniqueFileName);

            // Save file
            using (var fileStreamOutput = File.Create(filePath))
            {
                await fileStream.CopyToAsync(fileStreamOutput);
            }

            return GetFileUrl(uniqueFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<List<string>> UploadMultipleFilesAsync(List<(Stream stream, string fileName, string contentType)> files)
    {
        var urls = new List<string>();

        foreach (var file in files)
        {
            var url = await UploadFileAsync(file.stream, file.fileName, file.contentType);
            urls.Add(url);
        }

        return urls;
    }

    public Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_uploadPath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return Task.FromResult(false);
        }
    }

    public string GetFileUrl(string fileName)
    {
        return $"{_baseUrl}/{fileName}";
    }
}

// Cloud storage service (for production) - Azure Blob Storage
public class CloudFileUploadService : IFileUploadService
{
    private readonly ILogger<CloudFileUploadService> _logger;
    private readonly IConfiguration _configuration;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly string _cdnUrl;

    public CloudFileUploadService(IConfiguration configuration, ILogger<CloudFileUploadService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var connectionString = configuration["AzureStorage:ConnectionString"] 
            ?? Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        _containerName = configuration["AzureStorage:ContainerName"] ?? "uploads";
        _cdnUrl = configuration["AzureStorage:CdnUrl"] ?? "";
        
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("Azure Storage connection string is not configured. Check AzureStorage:ConnectionString in appsettings or AZURE_STORAGE_CONNECTION_STRING environment variable.");
            throw new InvalidOperationException("Azure Storage connection string is not configured");
        }
        
        try
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            
            // Ensure container exists
            EnsureContainerExistsAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Blob Storage client");
            throw new InvalidOperationException("Failed to initialize Azure Blob Storage", ex);
        }
    }

    private async Task EnsureContainerExistsAsync()
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            _logger.LogInformation("Azure Blob container '{ContainerName}' is ready", _containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure container exists");
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            // Generate unique filename
            var extension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            // Upload with content type
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            _logger.LogInformation("Uploaded file {FileName} to Azure Blob Storage", uniqueFileName);
            
            return GetFileUrl(uniqueFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to Azure Blob Storage: {FileName}", fileName);
            throw;
        }
    }

    public async Task<List<string>> UploadMultipleFilesAsync(List<(Stream stream, string fileName, string contentType)> files)
    {
        var urls = new List<string>();

        foreach (var file in files)
        {
            var url = await UploadFileAsync(file.stream, file.fileName, file.contentType);
            urls.Add(url);
        }

        return urls;
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            // Extract blob name from URL
            var uri = new Uri(fileUrl);
            var blobName = Path.GetFileName(uri.LocalPath);
            
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var result = await blobClient.DeleteIfExistsAsync();
            
            if (result.Value)
            {
                _logger.LogInformation("Deleted file {BlobName} from Azure Blob Storage", blobName);
            }
            
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from Azure Blob Storage: {FileUrl}", fileUrl);
            return false;
        }
    }

    public string GetFileUrl(string fileName)
    {
        // Use CDN URL if configured, otherwise use blob storage URL
        if (!string.IsNullOrEmpty(_cdnUrl))
        {
            return $"{_cdnUrl.TrimEnd('/')}/{fileName}";
        }
        
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(fileName);
        return blobClient.Uri.ToString();
    }
}

// Hybrid service: tries cloud, falls back to local
public class HybridFileUploadService : IFileUploadService
{
    private readonly IAppConfigService _configService;
    private readonly ILogger<HybridFileUploadService> _logger;
    private readonly LocalFileUploadService _localService;
    private readonly IConfiguration _configuration;

    public HybridFileUploadService(
        IAppConfigService configService,
        IConfiguration configuration,
        ILogger<HybridFileUploadService> logger,
        ILogger<LocalFileUploadService> localLogger)
    {
        _configService = configService;
        _configuration = configuration;
        _logger = logger;
        _localService = new LocalFileUploadService(configuration, localLogger);
    }

    private async Task<bool> IsCloudConfiguredAsync()
    {
        try
        {
            var useCloud = await _configService.GetConfigValueAsync<bool>("FileUpload:UseCloud", false);
            if (!useCloud) return false;

            // Check if cloud credentials are configured
            var cloudType = await _configService.GetConfigValueAsync("FileUpload:CloudType"); // e.g., "Azure", "AWS"
            return !string.IsNullOrWhiteSpace(cloudType);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            if (await IsCloudConfiguredAsync())
            {
                // TODO: Implement actual cloud upload when configured
                _logger.LogInformation("Cloud upload would be used here, falling back to local storage");
                // For now, fall through to local
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cloud upload failed, falling back to local storage for file: {FileName}", fileName);
        }

        // Fallback to local storage
        return await _localService.UploadFileAsync(fileStream, fileName, contentType);
    }

    public async Task<List<string>> UploadMultipleFilesAsync(List<(Stream stream, string fileName, string contentType)> files)
    {
        try
        {
            if (await IsCloudConfiguredAsync())
            {
                _logger.LogInformation("Cloud upload would be used here, falling back to local storage");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cloud upload failed, falling back to local storage for {Count} files", files.Count);
        }

        // Fallback to local storage
        return await _localService.UploadMultipleFilesAsync(files);
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            if (await IsCloudConfiguredAsync())
            {
                _logger.LogInformation("Cloud delete would be used here, falling back to local storage");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cloud delete failed, falling back to local storage for file: {FileUrl}", fileUrl);
        }

        // Fallback to local storage
        return await _localService.DeleteFileAsync(fileUrl);
    }

    public string GetFileUrl(string fileName)
    {
        // Local URLs for now
        return _localService.GetFileUrl(fileName);
    }
}
