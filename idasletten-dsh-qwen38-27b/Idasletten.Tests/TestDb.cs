using Idasletten.Data;
using Idasletten.Scoring;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Idasletten.Tests;

/// <summary>
/// A fresh, migrated, in-memory database plus a service provider wired like
/// the real app (MediatR + scoring engines + optional HttpContext access).
/// Use for CQRS-level tests where each test wants a clean, deterministic DB.
/// </summary>
public sealed class TestDb : IAsyncDisposable
{
    public AppDbContext Db { get; }
    public ServiceProvider Services { get; }
    private readonly SqliteConnection _connection;

    private TestDb(AppDbContext db, ServiceProvider services, SqliteConnection connection)
    {
        Db = db;
        Services = services;
        _connection = connection;
    }

    public static async Task<TestDb> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["AzureAd:ClientId"] = "" })
                .Build());
        services.AddSingleton(connection);
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        services.AddSingleton<IScoringEngine, EloScoring>();
        services.AddSingleton<IScoringEngine, TrueSkillScoring>();
        services.AddSingleton<IScoringEngine, LivesScoring>();
        services.AddSingleton<IScoringEngine, WinCountScoring>();
        services.AddSingleton<ScoringEngine>();
        services.AddHttpClient<Idasletten.GraphAvatarService>();
        services.AddScoped<Idasletten.ITokenAcquirer>(sp => new Idasletten.TokenAcquirer(
            null,
            sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
            sp.GetRequiredService<System.Net.Http.IHttpClientFactory>()));
        services.AddScoped(sp => sp.GetRequiredService<Idasletten.GraphAvatarService>().AsGraphAvatarService());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        return new TestDb(db, provider, connection);
    }

    /// <summary>Inserts the tournament plus its players; each user is persisted when given.</summary>
    public async Task AddTournamentAsync(Models.Tournament tournament,
        params (Models.User? User, Models.TournamentPlayer Player)[] players)
    {
        Db.Tournaments.Add(tournament);
        var added = new HashSet<Guid>();
        foreach (var (user, player) in players)
        {
            if (user is not null && added.Add(user.Id)) Db.Users.Add(user);
            player.TournamentId = tournament.Id;
            Db.TournamentPlayers.Add(player);
        }
        await Db.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        Db.Dispose();
        await _connection.CloseAsync();
        _connection.Dispose();
    }
}
