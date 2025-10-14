using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StarApi.Services;

public interface IDigitalOceanSpacesService
{
    Task<string> UploadAsync(Stream input, string key, string contentType, bool isPublic = true, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    string GetFileUrl(string key);
}
