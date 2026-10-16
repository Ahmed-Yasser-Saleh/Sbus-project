using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using NSubstitute;

using SBus.Application.SubcutaneousTests.Common;
using SBus.Infrastructure.Data;

using Xunit;

namespace SBus.Application.SubcutaneousTests.Safety;

public sealed class TestDatabaseSafetyTests
{
    [Theory]
    [InlineData("localhost", "sbus")]
    [InlineData("127.0.0.1", "postgres")]
    [InlineData("::1", "SBUS_TESTS")]
    [InlineData("localhost", "")]
    [InlineData("production.example.com", "sbus_tests")]
    [InlineData("192.0.2.10", "sbus_tests")]
    [InlineData("localhost,production.example.com", "sbus_tests")]
    [InlineData("localhost.evil.example", "sbus_tests")]
    [InlineData("/var/run/postgresql", "sbus_tests")]
    public void Guard_RejectsUnsafeActualEfConnectionWithoutOpeningIt(string host, string database)
    {
        using var context = Context(host, database);
        var connection = context.Database.GetDbConnection();
        var exception = Assert.Throws<InvalidOperationException>(() => TestDatabaseTarget.EnsureAllowed(connection));
        Assert.Contains("sbus_tests", exception.Message);
        Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("[::1]")]
    public void Guard_AllowsOnlyNamedTestDatabaseOnExplicitLocalHosts(string host)
    {
        using var context = Context(host, "sbus_tests");
        TestDatabaseTarget.EnsureAllowed(context.Database.GetDbConnection());
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }

    [Fact]
    public void Resolver_DevConnectionAloneNeverProvidesATestConnection()
    {
        var config = Configuration(("DefaultConnection", "Host=localhost;Database=sbus;Username=dev"));
        Assert.Null(TestDatabase.Resolve(config, null));
        Assert.Null(TestDatabase.Resolve(config, string.Empty));
    }

    [Fact]
    public void Resolver_UsesDedicatedSettingWithoutRewritingDatabase()
    {
        var config = Configuration(
            ("DefaultConnection", "Host=production.example.com;Database=sbus;Username=dev"),
            (TestDatabase.ConnectionName, "Host=127.0.0.1;Database=sbus_tests;Username=test"));
        var resolved = new Npgsql.NpgsqlConnectionStringBuilder(TestDatabase.Resolve(config, null));
        Assert.Equal("sbus_tests", resolved.Database);
        Assert.Equal("127.0.0.1", resolved.Host);
        Assert.Equal("test", resolved.Username);
    }

    [Fact]
    public void Resolver_DedicatedEnvironmentSettingTakesPrecedence()
    {
        var config = Configuration((TestDatabase.ConnectionName, "Host=localhost;Database=sbus_tests;Username=secret-test-user"));
        var resolved = new Npgsql.NpgsqlConnectionStringBuilder(TestDatabase.Resolve(config, "Host=::1;Database=sbus_tests;Username=environment-test-user"));
        Assert.Equal("::1", resolved.Host);
        Assert.Equal("environment-test-user", resolved.Username);
    }

    [Theory]
    [InlineData("Host=localhost;Database=sbus;Username=test")]
    [InlineData("Host=production.example.com;Database=sbus_tests;Username=test")]
    public void Resolver_UnsafeDedicatedSettingHardFailsInsteadOfRewritingOrFallingBack(string unsafeConnection)
    {
        var config = Configuration((TestDatabase.ConnectionName, unsafeConnection));
        Assert.Throws<InvalidOperationException>(() => TestDatabase.Resolve(config, null));
        var validConfig = Configuration((TestDatabase.ConnectionName, "Host=localhost;Database=sbus_tests;Username=test"));
        Assert.Throws<InvalidOperationException>(() => TestDatabase.Resolve(validConfig, unsafeConnection));
    }

    private static AppDbContext Context(string host, string database)
    {
        var connection = new Npgsql.NpgsqlConnectionStringBuilder { Host = host, Database = database, Username = "test" };
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).Options;
        return new AppDbContext(options, Substitute.For<IMediator>());
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] entries) =>
        new ConfigurationBuilder().AddInMemoryCollection(entries.ToDictionary(x => "ConnectionStrings:" + x.Key, x => (string?)x.Value)).Build();
}