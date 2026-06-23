using System.Net;
using System.Net.Http.Json;
using Idasletten.Shared;

namespace Idasletten.Tests;

public class HomeAndTournamentsTests : IClassFixture<IdaslettenWebFactory>
{
    private readonly HttpClient _client;
    public HomeAndTournamentsTests(IdaslettenWebFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Should_ReturnHome_When_NavigatingRoot()
    {
        var res = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Idasletten", body);
        Assert.Contains("Ragnarok", body);
    }

    [Fact]
    public async Task Should_HideCreateButton_When_NotAuthenticated()
    {
        var res = await _client.GetAsync("/Tournaments");
        var body = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Create tournament", body);
    }

    [Fact]
    public async Task Should_ShowSeededTournament_When_NavigatingTournamentsIndex()
    {
        var res = await _client.GetAsync("/Tournaments");
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Ragnarok Series", body);
    }
}