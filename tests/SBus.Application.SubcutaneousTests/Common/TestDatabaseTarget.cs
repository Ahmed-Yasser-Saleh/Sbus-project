using System.Data.Common;

using Npgsql;

namespace SBus.Application.SubcutaneousTests.Common;

internal static class TestDatabaseTarget
{
    private static readonly string[] LocalHosts = ["localhost", "127.0.0.1", "::1", "[::1]"];

    public static void EnsureAllowed(DbConnection connection)
    {
        // Npgsql's live Host property is unavailable before opening the connection.
        // Check the actual EF connection's settings without contacting any server.
        var settings = connection is NpgsqlConnection ? new NpgsqlConnectionStringBuilder(connection.ConnectionString) : null;
        if (settings is null
            || !string.Equals(connection.Database, "sbus_tests", StringComparison.Ordinal)
            || !string.Equals(settings.Database, "sbus_tests", StringComparison.Ordinal)
            || !LocalHosts.Contains(settings.Host, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Destructive database tests require exactly sbus_tests on localhost, 127.0.0.1 or ::1.");
        }
    }
}