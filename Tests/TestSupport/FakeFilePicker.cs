using OutMapper;

namespace TestSupport;

/// <summary>
/// <see cref="IFilePicker"/> fake that returns a preconfigured path instead of showing a live dialog.
/// </summary>
public sealed class FakeFilePicker : IFilePicker
{
    public string? PathToReturn { get; set; }

    public Task<string?> PickFileAsync() => Task.FromResult(PathToReturn);
}
