namespace Idasletten.Tests;

public class HomePageTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public HomePageTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Should_ShowPublicSeededTournaments_When_VisitingHome()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var html = await client.GetStringAsync("/");

        // Assert
        Assert.Contains("Valkyrior Open", html);
        Assert.Contains("Ragnarok Cup", html);
        Assert.DoesNotContain("Jotunheim League", html); // private tournament
    }

    [Fact]
    public async Task Should_ShowHeroQuoteAndCreateLink_When_VisitingHome()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var html = await client.GetStringAsync("/");

        // Assert
        Assert.Contains("»", html); // the verbatim Danish quote
        Assert.Contains("New tournament", html);
        Assert.Contains("Tournaments/Create", html, StringComparison.OrdinalIgnoreCase);
    }
}
