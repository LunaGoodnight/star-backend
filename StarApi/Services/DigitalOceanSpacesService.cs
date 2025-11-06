using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StarApi.Services;

public class DigitalOceanSpacesService : IDigitalOceanSpacesService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<DigitalOceanSpacesService> _logger;
    private readonly SpacesOptions _options;

    public DigitalOceanSpacesService(IOptions<SpacesOptions> options, ILogger<DigitalOceanSpacesService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var s3Config = new AmazonS3Config
        {
            ServiceURL = _options.Endpoint, // e.g., https://sgp1.digitaloceanspaces.com
            ForcePathStyle = false, // Use virtual-hosted style for DigitalOcean Spaces
            SignatureVersion = "v4",
            UseHttp = _options.UseHttp
        };

        var creds = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
        _s3Client = new AmazonS3Client(creds, s3Config);
    }

    public async Task<string> UploadAsync(Stream input, string key, string contentType, bool isPublic = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required", nameof(key));
        if (string.IsNullOrWhiteSpace(contentType)) contentType = "application/octet-stream";

        var putRequest = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = input,
            ContentType = contentType,
        };

        if (isPublic)
        {
            putRequest.CannedACL = S3CannedACL.PublicRead;
        }

        var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        _logger.LogInformation("Uploaded object {Key} to bucket {Bucket}. HTTP {StatusCode}", key, _options.Bucket, response.HttpStatusCode);

        return GetFileUrl(key);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key
        }, cancellationToken);
        _logger.LogInformation("Deleted object {Key} from bucket {Bucket}", key, _options.Bucket);
    }

    public string GetFileUrl(string key)
    {
        if (!string.IsNullOrWhiteSpace(_options.CdnBaseUrl))
        {
            return $"{_options.CdnBaseUrl.TrimEnd('/')}/{Uri.EscapeUriString(key)}";
        }
        // Default Spaces URL: https://{bucket}.{region}.digitaloceanspaces.com/{key}
        if (!string.IsNullOrWhiteSpace(_options.Endpoint) && _options.Endpoint.StartsWith("http"))
        {
            var endpoint = _options.Endpoint.TrimEnd('/');
            return $"{endpoint}/{_options.Bucket}/{Uri.EscapeUriString(key)}";
        }
        return $"https://{_options.Bucket}.digitaloceanspaces.com/{Uri.EscapeUriString(key)}";
    }
}

public class SpacesOptions
{
    public string Endpoint { get; set; } = string.Empty; // e.g., https://nyc3.digitaloceanspaces.com
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string CdnBaseUrl { get; set; } = string.Empty; // Optional, e.g., https://cdn.example.com
    public bool UseHttp { get; set; } = false;
}
