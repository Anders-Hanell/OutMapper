namespace TaskManager;

/// <summary>
/// Real <see cref="IFileSystem"/> implementation backed by <see cref="System.IO"/>. Stateless, so the
/// shared <see cref="Instance"/> is safe to use across concurrent requests.
/// </summary>
public sealed class LocalFileSystem : IFileSystem
{
    public static readonly LocalFileSystem Instance = new();

    private LocalFileSystem()
    {
    }

    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public string[] GetDirectories(string path) => Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);

    public string[] GetFiles(string path, string searchPattern) =>
        Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);

    public void CreateEmptyFile(string path)
    {
        using (File.Create(path))
        {
        }
    }

    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

    public Task<byte[]> ReadAllBytesAsync(string path) => File.ReadAllBytesAsync(path);

    public void WriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);

    public Task WriteAllBytesAsync(string path, byte[] bytes) => File.WriteAllBytesAsync(path, bytes);

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite) =>
        File.Copy(sourcePath, destinationPath, overwrite);

    public Stream OpenWrite(string path) => File.OpenWrite(path);
}
