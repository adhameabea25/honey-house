using HoneyHouse.Application.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace HoneyHouse.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    public const string Section = "FileStorage";
    /// <summary>Physical root folder (served as static files under RequestPath).</summary>
    public string RootPath { get; set; } = "wwwroot/uploads";
    public string RequestPath { get; set; } = "/uploads";
}

/// <summary>Stores uploads on local disk. Swap for a blob-storage implementation in production without touching callers.</summary>
public sealed class LocalFileStorage(IOptions<FileStorageOptions> options) : IFileStorage
{
    private readonly FileStorageOptions _options = options.Value;

    public async Task<string> SaveAsync(Stream content, string fileName, string folder, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) ext = ".jpg";
        var safeFolder = string.Concat(folder.Where(char.IsLetterOrDigit));
        var name = $"{Guid.NewGuid():N}{ext}";

        var dir = Path.Combine(Path.GetFullPath(_options.RootPath), safeFolder);
        Directory.CreateDirectory(dir);
        await using (var file = File.Create(Path.Combine(dir, name)))
            await content.CopyToAsync(file, ct);

        return $"{_options.RequestPath.TrimEnd('/')}/{safeFolder}/{name}";
    }

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        var prefix = _options.RequestPath.TrimEnd('/') + "/";
        if (!url.StartsWith(prefix, StringComparison.Ordinal)) return Task.CompletedTask;

        var root = Path.GetFullPath(_options.RootPath);
        var path = Path.GetFullPath(Path.Combine(root, url[prefix.Length..]));
        if (path.StartsWith(root, StringComparison.Ordinal) && File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
