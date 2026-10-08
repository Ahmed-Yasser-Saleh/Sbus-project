namespace SBus.Application.Common.Interfaces;

public interface IReceiptStorage
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken);

    Stream? OpenRead(string fileName);
}
