using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OutMapper;

/// <summary>
/// Real <see cref="IFolderOpener"/> implementation for the Skia Desktop head. Shells out to the
/// operating system's file explorer directly, since <c>Windows.System.Launcher.LaunchFolderPathAsync</c>
/// is not implemented by Uno Platform on this target.
/// </summary>
internal sealed class DesktopFolderOpener : IFolderOpener
{
    public Task<bool> OpenFolderAsync(string folderPath)
    {
        var (fileName, arguments) = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ("explorer.exe", $"\"{folderPath}\"")
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? ("open", $"\"{folderPath}\"")
                : ("xdg-open", $"\"{folderPath}\"");

        try
        {
            using var process = Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = false });
            return Task.FromResult(process is not null);
        }
        catch (Exception exception) when (exception is Win32Exception or System.IO.IOException)
        {
            return Task.FromResult(false);
        }
    }
}
