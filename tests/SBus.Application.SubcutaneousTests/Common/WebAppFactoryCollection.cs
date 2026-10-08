using Xunit;

namespace SBus.Application.SubcutaneousTests.Common;

[CollectionDefinition(Name)]
public class WebAppFactoryCollection : ICollectionFixture<WebAppFactory>
{
    public const string Name = "WebAppFactoryCollection";
}
