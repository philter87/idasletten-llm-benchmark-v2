namespace Idasletten.Tests;

public class TournamentsPageTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public TournamentsPageTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Should_HideChildRounds_When_NoIncludeFlag()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var html = await client.GetStringAsync("/tournaments");

        // Assert — the seeded child round "Valkyrior Open — Round 2" is hidden
        Assert.Contains("Valkyrior Open", html);
        Assert.DoesNotContain("Round 2", html);
    }

    [Fact]
    public async Task Should_ShowChildRounds_When_IncludeFlagIsSet()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var html = await client.GetStringAsync("/tournaments?includeChildren=true");

        // Assert
        Assert.Contains("Round 2", html);
    }
}
