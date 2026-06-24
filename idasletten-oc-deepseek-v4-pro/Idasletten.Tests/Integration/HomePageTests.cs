namespace Idasletten.Tests.Integration;

public class HomePageTests : IClassFixture<IdaslettenWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IdaslettenWebApplicationFactory _factory;

    public HomePageTests(IdaslettenWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_HomePageLoaded()
    {
        var response = await _client.GetAsync("/");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_TournamentsPageLoaded()
    {
        var response = await _client.GetAsync("/Tournaments");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Should_NotReturnSuccess_When_CreatingTournamentUnauthenticated()
    {
        var response = await _client.GetAsync("/Tournaments/Create");
        Assert.False(response.IsSuccessStatusCode);
    }
}
