using System.Net;
using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Players.Queries;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;

namespace Idasletten.Tests;

public class PageTests(IdaslettenFactory factory) : IClassFixture<IdaslettenFactory>, IAsyncLifetime
{
    /// <summary>Makes sure the database is migrated and seeded before the first test runs.</summary>
    public Task InitializeAsync() => factory.InitialiseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_ShowTheNorseQuoteAndPublicTournaments_When_TheFrontPageIsOpened()
    {
        // Arrange
        var client = factory.CreateWebClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Hver aften er Idasletten rød af blod", html);
        Assert.Contains("Ragnarok Cup 2026", html);
        Assert.DoesNotContain("Midgard Mesterskab 2025", html);
    }

    [Fact]
    public async Task Should_ListArchivedTournaments_When_TheHistoricalPageIsOpened()
    {
        // Arrange
        var client = factory.CreateWebClient();

        // Act
        var html = await client.GetStringAsync("/tournaments");

        // Assert
        Assert.Contains("Midgard Mesterskab 2025", html);
        Assert.Contains("Arkiveret", html);
    }

    [Fact]
    public async Task Should_RedirectToLogin_When_AnonymousOpensCreateTournament()
    {
        // Arrange
        var client = factory.CreateWebClient();

        // Act
        var response = await client.GetAsync("/tournaments/create");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Should_OpenCreateTournament_When_TheUserIsLoggedIn()
    {
        // Arrange
        var client = await factory.CreateLoggedInClientAsync();

        // Act
        var response = await client.GetAsync("/tournaments/create");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_ShowTheTestLogin_When_TheTestUserEnvironmentIsSet()
    {
        // Arrange
        var client = factory.CreateWebClient();

        // Act
        var html = await client.GetStringAsync("/login");

        // Assert
        Assert.Contains("Log ind som testbruger", html);
    }

    [Fact]
    public async Task Should_SaveTheResult_When_AnonymousRecordsAMatch()
    {
        // Arrange - no login needed to record a result.
        var client = factory.CreateWebClient();
        var tournamentId = await factory.SendAsync(new CreateTournament("Anonym kamp", TeamSize: 1));
        var matchPage = await client.GetStringAsync($"/tournaments/{tournamentId}/create-match");
        var initials = (Any.Initials(), Any.Initials());

        // Act
        var response = await client.PostAsync(
            $"/tournaments/{tournamentId}/create-match",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Teams[0].Initials[0]"] = initials.Item1,
                ["Teams[0].Goals"] = "10",
                ["Teams[1].Initials[0]"] = initials.Item2,
                ["Teams[1].Goals"] = "7",
                ["planned"] = "false",
                ["__RequestVerificationToken"] = IdaslettenFactory.AntiforgeryToken(matchPage),
            }));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var matches = await factory.SendAsync(new GetMatches(tournamentId));
        Assert.Single(matches.Played);
        Assert.Equal("10 - 7", matches.Played.Single().ScoreLine);
    }

    [Fact]
    public async Task Should_ShowTheMatchAsReadOnly_When_AnonymousOpensAPlayedMatch()
    {
        // Arrange
        var (tournamentId, matchId) = await PlayedMatchAsync();
        var client = factory.CreateWebClient();

        // Act
        var html = await client.GetStringAsync(
            $"/tournaments/{tournamentId}/create-match?matchId={matchId}");

        // Assert
        Assert.Contains("Kampen er allerede spillet", html);
        Assert.Contains("disabled", html);
    }

    [Fact]
    public async Task Should_RejectTheEdit_When_AnonymousChangesAPlayedMatch()
    {
        // Arrange
        var (tournamentId, matchId) = await PlayedMatchAsync();
        var client = factory.CreateWebClient();
        var page = await client.GetStringAsync($"/tournaments/{tournamentId}/create-match");

        // Act
        var response = await client.PostAsync(
            $"/tournaments/{tournamentId}/create-match?matchId={matchId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Teams[0].Initials[0]"] = "AAA",
                ["Teams[0].Goals"] = "1",
                ["Teams[1].Initials[0]"] = "BBB",
                ["Teams[1].Goals"] = "10",
                ["planned"] = "false",
                ["__RequestVerificationToken"] = IdaslettenFactory.AntiforgeryToken(page),
            }));

        // Assert - the anonymous user is sent to the login page instead.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.OriginalString);

        var match = await factory.SendAsync(new GetMatch(tournamentId, matchId));
        Assert.Equal("10 - 7", match!.ScoreLine);
    }

    [Fact]
    public async Task Should_AllowTheEdit_When_ALoggedInUserChangesAPlayedMatch()
    {
        // Arrange
        var (tournamentId, matchId) = await PlayedMatchAsync();
        var client = await factory.CreateLoggedInClientAsync();
        var page = await client.GetStringAsync(
            $"/tournaments/{tournamentId}/create-match?matchId={matchId}");
        var match = await factory.SendAsync(new GetMatch(tournamentId, matchId));
        var teams = match!.Teams;

        // Act
        var response = await client.PostAsync(
            $"/tournaments/{tournamentId}/create-match?matchId={matchId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Teams[0].Initials[0]"] = teams[0].Players[0].Initials,
                ["Teams[0].Goals"] = "3",
                ["Teams[1].Initials[0]"] = teams[1].Players[0].Initials,
                ["Teams[1].Goals"] = "10",
                ["planned"] = "false",
                ["__RequestVerificationToken"] = IdaslettenFactory.AntiforgeryToken(page),
            }));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var updated = await factory.SendAsync(new GetMatch(tournamentId, matchId));
        Assert.Equal("3 - 10", updated!.ScoreLine);
    }

    [Fact]
    public async Task Should_ShowTheScoreboard_When_ATournamentIsOpened()
    {
        // Arrange
        var summary = (await factory.SendAsync(new GetTournaments()))
            .First(tournament => tournament.Name == "Ragnarok Cup 2026");
        var client = factory.CreateWebClient();

        // Act
        var html = await client.GetStringAsync($"/tournaments/{summary.Id}");

        // Assert
        Assert.Contains("Stilling", html);
        Assert.Contains("Opret kamp", html);
        Assert.Contains("THO", html);
    }

    [Fact]
    public async Task Should_ShowCrossTournamentStats_When_APlayerPageIsOpened()
    {
        // Arrange
        var summary = (await factory.SendAsync(new GetTournaments()))
            .First(tournament => tournament.Name == "Ragnarok Cup 2026");
        var player = (await factory.SendAsync(new GetScoreboard(summary.Id))).First();
        var client = factory.CreateWebClient();

        // Act
        var html = await client.GetStringAsync($"/users/{player.UserId}");

        // Assert
        Assert.Contains("På tværs af alle turneringer", html);
        Assert.Contains(summary.Name, html);
    }

    [Fact]
    public async Task Should_AddThePlayer_When_ThePlayerDialogIsPosted()
    {
        // Arrange
        var tournamentId = await factory.SendAsync(new CreateTournament("Dialog turnering"));
        var client = factory.CreateWebClient();
        var page = await client.GetStringAsync($"/tournaments/{tournamentId}/players");
        var initials = Any.Initials();

        // Act
        var response = await client.PostAsync(
            $"/tournaments/{tournamentId}/players?handler=AddPlayer",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["initials"] = initials,
                ["name"] = "Ny Spiller",
                ["__RequestVerificationToken"] = IdaslettenFactory.AntiforgeryToken(page),
            }));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var scoreboard = await factory.SendAsync(new GetScoreboard(tournamentId));
        Assert.Contains(scoreboard, row => row.Initials == initials.ToUpperInvariant());
    }

    private async Task<(Guid TournamentId, Guid MatchId)> PlayedMatchAsync()
    {
        var tournamentId = await factory.SendAsync(
            new CreateTournament(Any.Tournament().Name, TeamSize: 1));

        var home = Any.Initials();
        var away = Any.Initials();
        while (away == home)
        {
            away = Any.Initials();
        }

        await factory.SendAsync(new AddPlayerToTournament(tournamentId, home));
        await factory.SendAsync(new AddPlayerToTournament(tournamentId, away));

        var matchId = await factory.SendAsync(new SaveMatch(tournamentId, Guid.NewGuid(),
        [
            new MatchTeamInput([home], 10),
            new MatchTeamInput([away], 7),
        ]));

        return (tournamentId, matchId);
    }
}
