namespace HoneyHouse.Application.Interfaces.Services;

public interface IFileStorage
{
    /// <summary>Saves the stream and returns its public URL.</summary>
    Task<string> SaveAsync(Stream content, string fileName, string folder, CancellationToken ct = default);
    Task DeleteAsync(string url, CancellationToken ct = default);
}
