using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

using SBus.Application.SubcutaneousTests.Common;

using Xunit;

namespace SBus.Application.SubcutaneousTests.Web;

[Collection(WebAppFactoryCollection.Name)]
public class OfficeAccessTests(WebAppFactory factory)
{
    private readonly WebAppFactory _factory = factory;

    [Theory]
    [InlineData("/Office")]
    [InlineData("/Office/Payments")]
    [InlineData("/Office/Settings/Schedules")]
    public async Task AnonymousVisitor_IsSentToTheOfficeLoginPage(string path)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.AbsolutePath);
    }
}
