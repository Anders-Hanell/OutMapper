using System.Text.RegularExpressions;
using TaskManager;
using Path = System.IO.Path;

namespace TestSupport;

/// <summary>
/// In-memory <see cref="IFileSystem"/> fake for tests. Each instance is independent, so parallel test
/// execution is inherently safe - there is no shared mutable state and no real disk access.
/// </summary>
public sealed class InMemoryFileSystem : IFileSystem
{
    private readonly HashSet<string> _directories = new(StringComparer.Ordinal);
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public bool FileExists(string path) => _files.ContainsKey(path);

    public bool DirectoryExists(string path) => _directories.Contains(path);

    public void CreateDirectory(string path)
    {
        for (var directory = path; !string.IsNullOrEmpty(directory); directory = Path.GetDirectoryName(directory))
        {
            if (!_directories.Add(directory))
            {
                break;
            }
        }
    }

    public string[] GetDirectories(string path)
    {
        return _directories
            .Where(directory => string.Equals(Path.GetDirectoryName(directory), path, StringComparison.Ordinal))
            .ToArray();
    }

    public string[] GetFiles(string path, string searchPattern)
    {
        var regex = ToRegex(searchPattern);
        return _files.Keys
            .Where(file => string.Equals(Path.GetDirectoryName(file), path, StringComparison.Ordinal) &&
                           regex.IsMatch(Path.GetFileName(file)))
            .ToArray();
    }

    public void CreateEmptyFile(string path)
    {
        CreateDirectory(Path.GetDirectoryName(path)!);
        _files[path] = [];
    }

    public byte[] ReadAllBytes(string path) => _files[path];

    public Task<byte[]> ReadAllBytesAsync(string path) => Task.FromResult(ReadAllBytes(path));

    public void WriteAllBytes(string path, byte[] bytes)
    {
        CreateDirectory(Path.GetDirectoryName(path)!);
        _files[path] = bytes;
    }

    public Task WriteAllBytesAsync(string path, byte[] bytes)
    {
        WriteAllBytes(path, bytes);
        return Task.CompletedTask;
    }

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
    {
        if (!overwrite && _files.ContainsKey(destinationPath))
        {
            throw new IOException($"The file '{destinationPath}' already exists.");
        }

        CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        _files[destinationPath] = _files[sourcePath];
    }

    public Stream OpenWrite(string path)
    {
        CreateDirectory(Path.GetDirectoryName(path)!);
        return new InMemoryWriteStream(this, path);
    }

    private static Regex ToRegex(string searchPattern)
    {
        var pattern = "^" + Regex.Escape(searchPattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return new Regex(pattern, RegexOptions.IgnoreCase);
    }

    private sealed class InMemoryWriteStream : MemoryStream
    {
        private readonly InMemoryFileSystem _owner;
        private readonly string _path;
        private bool _flushedToOwner;

        public InMemoryWriteStream(InMemoryFileSystem owner, string path)
        {
            _owner = owner;
            _path = path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_flushedToOwner)
            {
                _flushedToOwner = true;
                _owner._files[_path] = ToArray();
            }

            base.Dispose(disposing);
        }
    }
}
