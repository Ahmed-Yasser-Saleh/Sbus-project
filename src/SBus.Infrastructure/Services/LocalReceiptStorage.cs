using System.Text.RegularExpressions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using SBus.Application.Common.Interfaces;
using SBus.Infrastructure.Settings;

namespace SBus.Infrastructure.Services;

public partial class LocalReceiptStorage(IHostEnvironment environment, IOptions<AppSettings> options) : IReceiptStorage
{
    private readonly string _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.ReceiptsPath));

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_root);

        var fileName = $"{Guid.CreateVersion7():N}{extension.ToLowerInvariant()}";

        if (!SafeFileName().IsMatch(fileName))
        {
            throw new ArgumentException("Unsupported receipt extension.", nameof(extension));
        }

        await using var file = new FileStream(Path.Combine(_root, fileName), FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(file, cancellationToken);

        return fileName;
    }

    public Stream? OpenRead(string fileName)
    {
        if (!SafeFileName().IsMatch(fileName))
        {
            return null;
        }

        var path = Path.Combine(_root, fileName);

        return File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
    }

    [GeneratedRegex("^[a-f0-9]{32}\\.(jpg|png|webp|pdf)$")]
    private static partial Regex SafeFileName();
}
