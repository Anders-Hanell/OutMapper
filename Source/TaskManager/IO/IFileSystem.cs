namespace TaskManager;

/// <summary>
/// Seam over disk access used by <see cref="TaskManager"/> and <c>OutMapper</c> services, so tests can
/// substitute an in-memory implementation instead of touching real disk.
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    void CreateDirectory(string path);

    string[] GetDirectories(string path);

    string[] GetFiles(string path, string searchPattern);

    void CreateEmptyFile(string path);

    byte[] ReadAllBytes(string path);

    Task<byte[]> ReadAllBytesAsync(string path);

    void WriteAllBytes(string path, byte[] bytes);

    Task WriteAllBytesAsync(string path, byte[] bytes);

    void CopyFile(string sourcePath, string destinationPath, bool overwrite);

    Stream OpenWrite(string path);
}
