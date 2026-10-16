using Microsoft.Extensions.Configuration;

using Npgsql;

namespace SBus.Application.SubcutaneousTests.Common;

public static class TestDatabase
{
    public const string EnvironmentVariable = "SBUS_TEST_CONNECTION";
    public const string ConnectionName = "TestConnection";

    private static readonly Lazy<string?> Connection = new(Resolve);

    public static string? ConnectionString => Connection.Value;

    internal static string? Resolve(IConfiguration configuration, string? environmentConnection)
    {
        var connectionString = !string.IsNullOrWhiteSpace(environmentConnection)
            ? environmentConnection : configuration.GetConnectionString(ConnectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        using var connection = new NpgsqlConnection(connectionString);
        TestDatabaseTarget.EnsureAllowed(connection);
        return connection.ConnectionString;
    }

    private static string? Resolve()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .Build();

        return Resolve(configuration, Environment.GetEnvironmentVariable(EnvironmentVariable));
    }
}
