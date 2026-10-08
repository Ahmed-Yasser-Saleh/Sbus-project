namespace SBus.Tests.Common;

public sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public void SetUtcNow(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public void Advance(TimeSpan by)
    {
        _utcNow = _utcNow.Add(by);
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;
}
