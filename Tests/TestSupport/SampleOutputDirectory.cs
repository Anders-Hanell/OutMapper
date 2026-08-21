using Path = System.IO.Path;

namespace TestSupport;

/// <summary>
/// Locates the per-test output folder used by on-demand sample-file generators (e.g. PDF generators meant
/// for visual inspection). Each caller gets its own subfolder under OutMapperAutomatedTestsOutput at the
/// solution root, keyed by <paramref name="testName"/>, so concurrent generators never write to the same file.
/// </summary>
public static class SampleOutputDirectory
{
    public static string For(string testName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OutMapper.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new DirectoryNotFoundException("Could not locate OutMapper.sln above " + AppContext.BaseDirectory);
        }

        return Path.Combine(directory.FullName, "OutMapperAutomatedTestsOutput", testName);
    }
}
