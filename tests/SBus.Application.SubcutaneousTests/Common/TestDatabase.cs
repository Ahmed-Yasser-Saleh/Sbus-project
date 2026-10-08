using Microsoft.Extensions.Configuration;

using Npgsql;

namespace SBus.Application.SubcutaneousTests.Common;

public static class TestDatabase
{
    public const string EnvironmentVariable = "SBUS_TEST_CONNECTION";

    private static readonly Lazy<string?> Connection = new(Resolve);

    public static string? ConnectionString => Connection.Value;

    private static string? Resolve()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);

        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (string.IsNullOrEmpty(builder.Password))
        {
            return null;
        }

        builder.Database = "sbus_tests";

        return builder.ConnectionString;
    }
}
