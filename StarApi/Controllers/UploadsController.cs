using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarApi.Services;

namespace StarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadsController : ControllerBase
{
    private readonly IDigitalOceanSpacesService _spacesService;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(IDigitalOceanSpacesService spacesService, ILogger<UploadsController> logger)
    {
        _spacesService = spacesService;
        _logger = logger;
    }

    // POST: api/uploads
    [HttpPost]
    [Authorize]
    [RequestSizeLimit(20_000_000)] // 20 MB default limit, adjust as needed
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromQuery] string? prefix = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        // Basic content-type validation for images
        var allowed = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/avif" };
        if (!allowed.Contains(file.ContentType))
        {
            return BadRequest(new { error = "Unsupported file type. Allowed: jpg, png, gif, webp, avif" });
        }

        var key = BuildObjectKey(prefix, file.FileName);

        await using var stream = file.OpenReadStream();
        var url = await _spacesService.UploadAsync(stream, key, file.ContentType, isPublic: true, HttpContext.RequestAborted);

        return Ok(new { key, url, contentType = file.ContentType, size = file.Length });
    }

    // DELETE: api/uploads/{*key}
    [HttpDelete("{**key}")]
    [Authorize]
    public async Task<IActionResult> Delete(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(new { error = "Key is required" });
        }
        await _spacesService.DeleteAsync(key, HttpContext.RequestAborted);
        return NoContent();
    }

    private static string BuildObjectKey(string? prefix, string originalFileName)
    {
        var safeName = Path.GetFileNameWithoutExtension(originalFileName);
        var ext = Path.GetExtension(originalFileName);
        var slug = string.Join("-", safeName.Split(Path.GetInvalidFileNameChars().Concat(new[] { ' ' }).ToArray(), StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug)) slug = "file";
        var unique = Guid.NewGuid().ToString("n").Substring(0, 12);
        // Default to 'blog' folder and do not include date-based subfolders
        var basePrefix = string.IsNullOrWhiteSpace(prefix) ? "blog" : prefix.Trim('/');
        var key = $"{basePrefix}/{slug}-{unique}{ext}";
        return key.Replace(" ", "").Replace("\\", "/");
    }
}
