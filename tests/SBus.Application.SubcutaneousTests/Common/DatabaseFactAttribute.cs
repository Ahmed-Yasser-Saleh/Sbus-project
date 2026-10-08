using Xunit;

namespace SBus.Application.SubcutaneousTests.Common;

public sealed class DatabaseFactAttribute : FactAttribute
{
    public DatabaseFactAttribute()
    {
        if (TestDatabase.ConnectionString is null)
        {
            Skip = $"No test database. Set ConnectionStrings:DefaultConnection in the SBus.Web user-secrets or set {TestDatabase.EnvironmentVariable}.";
        }
    }
}
