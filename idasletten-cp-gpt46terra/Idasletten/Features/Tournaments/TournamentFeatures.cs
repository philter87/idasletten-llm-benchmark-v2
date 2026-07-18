using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments;

public record TournamentCreated(Guid TournamentId) : INotification;
public record PlayerAdded(Guid TournamentId, Guid PlayerId) : INotification;
public record MatchRecorded(Guid TournamentId, Guid MatchId) : INotification;
public record MatchesPlanned(Guid TournamentId, int Count) : INotification;

public record TournamentSummary(Guid Id, string Name, ScoreSystem ScoreSystem, bool IsPublic, bool IsArchived, int PlayerCount, int? RoundNumber);
public record PlayerRow(Guid Id, Guid UserId, string Initials, string Name, double Score, double ScoreDiff, int Won, int Lost, int Games, int? Lives, int PointsWon, int PointsLost);
public record MatchRow(Guid Id, int Order, MatchState State, string Teams, string Score, DateTimeOffset? PlayedAt);
public record TournamentDetail(
    Guid Id, string Name, ScoreSystem ScoreSystem, int TeamSize, int PointsToWin, bool IsArchived, bool IsPublic,
    IReadOnlyList<PlayerRow> Players, IReadOnlyList<MatchRow> Planned, IReadOnlyList<MatchRow> Recent);
public record TournamentMatchDetail(Guid Id, MatchState State, IReadOnlyList<string> FirstTeam, IReadOnlyList<string> SecondTeam, int? FirstScore, int? SecondScore);

public record GetTournamentsQuery(bool PublicOnly, bool IncludeChildren) : IRequest<IReadOnlyList<TournamentSummary>>;
public class GetTournamentsHandler(IdaslettenDbContext db) : IRequestHandler<GetTournamentsQuery, IReadOnlyList<TournamentSummary>>
{
    public async Task<IReadOnlyList<TournamentSummary>> Handle(GetTournamentsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Tournaments.AsNoTracking().AsQueryable();
        if (request.PublicOnly)
            query = query.Where(x => x.IsPublic && !x.IsArchived);
        if (!request.IncludeChildren)
            query = query.Where(x => x.ParentTournamentId == null);
        return await query.OrderBy(x => x.IsArchived).ThenBy(x => x.Name)
            .Select(x => new TournamentSummary(x.Id, x.Name, x.ScoreSystem, x.IsPublic, x.IsArchived, x.Players.Count, x.RoundNumber))
            .ToListAsync(cancellationToken);
    }
}

public record GetTournamentQuery(Guid TournamentId) : IRequest<TournamentDetail?>;
public class GetTournamentHandler(IdaslettenDbContext db) : IRequestHandler<GetTournamentQuery, TournamentDetail?>
{
    public async Task<TournamentDetail?> Handle(GetTournamentQuery request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.AsNoTracking()
            .Include(x => x.Players).ThenInclude(x => x.User)
            .Include(x => x.Matches).ThenInclude(x => x.Teams).ThenInclude(x => x.Team).ThenInclude(x => x.Players).ThenInclude(x => x.TournamentPlayer).ThenInclude(x => x.User)
            .Include(x => x.Matches).ThenInclude(x => x.Results).ThenInclude(x => x.Team)
            .SingleOrDefaultAsync(x => x.Id == request.TournamentId, cancellationToken);
        if (tournament is null) return null;

        var rows = tournament.Matches.Select(ToMatchRow).ToList();
        return new TournamentDetail(
            tournament.Id, tournament.Name, tournament.ScoreSystem, tournament.TeamSize, tournament.PointsToWin,
            tournament.IsArchived, tournament.IsPublic,
            tournament.Players.OrderByDescending(x => x.Score).ThenByDescending(x => x.PointsWon - x.PointsLost)
                .Select(x => new PlayerRow(x.Id, x.UserId, x.User.Username, x.User.Name, x.Score, x.ScoreDiff, x.WinCount, x.LoseCount, x.MatchCount, x.Lives, x.PointsWon, x.PointsLost)).ToList(),
            rows.Where(x => x.State == MatchState.Planned).OrderBy(x => x.Order).Take(5).ToList(),
            rows.Where(x => x.State == MatchState.Done).OrderByDescending(x => x.PlayedAt).Take(5).ToList());
    }

    internal static MatchRow ToMatchRow(TournamentMatch match)
    {
        var teams = match.Teams.OrderBy(x => x.Team.Number)
            .Select(x => string.Join(" + ", x.Team.Players.Select(p => p.TournamentPlayer.User.Username))).ToList();
        var score = match.Results.Any()
            ? string.Join(" – ", match.Results.OrderBy(x => x.Team.Number).Select(x => x.GoalsWon)) : "Planned";
        return new MatchRow(match.Id, match.Order, match.State, string.Join(" vs ", teams), score, match.PlayedAt);
    }
}

public record GetMatchQuery(Guid TournamentId, Guid MatchId) : IRequest<TournamentMatchDetail?>;
public class GetMatchHandler(IdaslettenDbContext db) : IRequestHandler<GetMatchQuery, TournamentMatchDetail?>
{
    public async Task<TournamentMatchDetail?> Handle(GetMatchQuery request, CancellationToken cancellationToken)
    {
        var match = await db.TournamentMatches.AsNoTracking()
            .Include(x => x.Teams).ThenInclude(x => x.Team).ThenInclude(x => x.Players).ThenInclude(x => x.TournamentPlayer).ThenInclude(x => x.User)
            .Include(x => x.Results)
            .SingleOrDefaultAsync(x => x.Id == request.MatchId && x.TournamentId == request.TournamentId, cancellationToken);
        if (match is null) return null;
        var teams = match.Teams.OrderBy(x => x.Team.Number).ToList();
        if (teams.Count < 2) return null;
        return new TournamentMatchDetail(match.Id, match.State,
            teams[0].Team.Players.Select(x => x.TournamentPlayer.User.Username).ToList(),
            teams[1].Team.Players.Select(x => x.TournamentPlayer.User.Username).ToList(),
            match.Results.SingleOrDefault(x => x.TeamId == teams[0].TeamId)?.GoalsWon,
            match.Results.SingleOrDefault(x => x.TeamId == teams[1].TeamId)?.GoalsWon);
    }
}

public record GetAllMatchesQuery(Guid TournamentId) : IRequest<IReadOnlyList<MatchRow>>;
public class GetAllMatchesHandler(IdaslettenDbContext db) : IRequestHandler<GetAllMatchesQuery, IReadOnlyList<MatchRow>>
{
    public async Task<IReadOnlyList<MatchRow>> Handle(GetAllMatchesQuery request, CancellationToken cancellationToken)
    {
        var matches = await db.TournamentMatches.AsNoTracking()
            .Where(x => x.TournamentId == request.TournamentId)
            .Include(x => x.Teams).ThenInclude(x => x.Team).ThenInclude(x => x.Players).ThenInclude(x => x.TournamentPlayer).ThenInclude(x => x.User)
            .Include(x => x.Results).ThenInclude(x => x.Team)
            .OrderBy(x => x.Order).ToListAsync(cancellationToken);
        return matches.Select(GetTournamentHandler.ToMatchRow).ToList();
    }
}

public record UserStats(Guid Id, string Initials, string Name, IReadOnlyList<PlayerRow> Results);
public record GetUserStatsQuery(Guid UserId) : IRequest<UserStats?>;
public class GetUserStatsHandler(IdaslettenDbContext db) : IRequestHandler<GetUserStatsQuery, UserStats?>
{
    public async Task<UserStats?> Handle(GetUserStatsQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().Include(x => x.TournamentPlayers).ThenInclude(x => x.Tournament)
            .SingleOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        return user is null ? null : new UserStats(user.Id, user.Username, user.Name,
            user.TournamentPlayers.Select(x => new PlayerRow(x.Id, user.Id, user.Username, x.Tournament.Name, x.Score, x.ScoreDiff, x.WinCount, x.LoseCount, x.MatchCount, x.Lives, x.PointsWon, x.PointsLost)).ToList());
    }
}

public record CreateTournamentCommand(string Name, int? MaxPlayerCount, int TeamSize, int PointsToWin, ScoreSystem ScoreSystem, bool IsPublic, Guid? ParentTournamentId = null) : IRequest<Guid>;
public class CreateTournamentHandler(IdaslettenDbContext db, IPublisher publisher) : IRequestHandler<CreateTournamentCommand, Guid>
{
    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = new Tournament
        {
            Name = request.Name.Trim(), MaxPlayerCount = request.MaxPlayerCount, TeamSize = request.TeamSize,
            PointsToWin = request.PointsToWin, ScoreSystem = request.ScoreSystem, IsPublic = request.IsPublic,
            ParentTournamentId = request.ParentTournamentId
        };
        if (request.ParentTournamentId is { } parentId)
        {
            var parent = await db.Tournaments.Include(x => x.Players).SingleAsync(x => x.Id == parentId, cancellationToken);
            tournament.RoundNumber = (parent.RoundNumber ?? 0) + 1;
            foreach (var player in parent.Players)
                tournament.Players.Add(new TournamentPlayer { UserId = player.UserId, Lives = request.ScoreSystem == ScoreSystem.Lives ? 3 : null });
        }
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.Publish(new TournamentCreated(tournament.Id), cancellationToken);
        return tournament.Id;
    }
}

public record AddPlayerCommand(Guid TournamentId, string Initials, string? Name) : IRequest<Guid>;
public class AddPlayerHandler(IdaslettenDbContext db, IPublisher publisher) : IRequestHandler<AddPlayerCommand, Guid>
{
    public async Task<Guid> Handle(AddPlayerCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.Include(x => x.Players).SingleAsync(x => x.Id == request.TournamentId, cancellationToken);
        if (tournament.MaxPlayerCount is { } max && tournament.Players.Count >= max)
            throw new InvalidOperationException($"This tournament is limited to {max} players.");
        var initials = request.Initials.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == initials, cancellationToken);
        if (user is null)
        {
            user = new User { Username = initials, Name = string.IsNullOrWhiteSpace(request.Name) ? initials : request.Name.Trim() };
            db.Users.Add(user);
        }
        var existing = tournament.Players.SingleOrDefault(x => x.UserId == user.Id);
        if (existing is not null) return existing.Id;
        var player = new TournamentPlayer { User = user, Tournament = tournament, Lives = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : null };
        db.TournamentPlayers.Add(player);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.Publish(new PlayerAdded(tournament.Id, player.Id), cancellationToken);
        return player.Id;
    }
}

public record SaveMatchCommand(Guid TournamentId, Guid? MatchId, IReadOnlyList<IReadOnlyList<string>> Teams, IReadOnlyList<int> Scores, bool IsPlanned = false) : IRequest<Guid>;
public class SaveMatchHandler(IdaslettenDbContext db, ScoreCalculator calculator, IPublisher publisher) : IRequestHandler<SaveMatchCommand, Guid>
{
    public async Task<Guid> Handle(SaveMatchCommand request, CancellationToken cancellationToken)
    {
        if (request.Teams.Count < 2 || request.Teams.Any(x => x.Count == 0))
            throw new InvalidOperationException("A match needs at least two teams with players.");
        var tournament = await db.Tournaments.SingleAsync(x => x.Id == request.TournamentId, cancellationToken);
        var match = request.MatchId is { } matchId
            ? await db.TournamentMatches.Include(x => x.Teams).SingleAsync(x => x.Id == matchId && x.TournamentId == request.TournamentId, cancellationToken)
            : new TournamentMatch { TournamentId = tournament.Id, Order = (await db.TournamentMatches.Where(x => x.TournamentId == tournament.Id).MaxAsync(x => (int?)x.Order, cancellationToken) ?? 0) + 1 };
        if (request.MatchId is null) db.TournamentMatches.Add(match);
        if (request.MatchId is not null) db.RemoveRange(match.Teams);
        var teamIds = new List<Guid>();
        var nextTeamNumber = (await db.TournamentTeams.Where(x => x.TournamentId == tournament.Id)
            .MaxAsync(x => (int?)x.Number, cancellationToken) ?? 0) + 1;
        for (var teamIndex = 0; teamIndex < request.Teams.Count; teamIndex++)
        {
            var members = new List<TournamentPlayer>();
            foreach (var initials in request.Teams[teamIndex])
                members.Add(await FindOrCreatePlayer(tournament, initials, cancellationToken));
            var team = CreateTeam(tournament, members, nextTeamNumber++);
            db.Add(new TournamentMatchTeam { Match = match, Team = team });
            teamIds.Add(team.Id);
        }
        db.RemoveRange(db.TournamentTeamMatchResults.Where(x => x.MatchId == match.Id));
        match.State = request.IsPlanned ? MatchState.Planned : MatchState.Done;
        match.PlayedAt = request.IsPlanned ? null : DateTimeOffset.UtcNow;
        if (!request.IsPlanned)
        {
            for (var i = 0; i < teamIds.Count; i++)
            {
                var score = request.Scores.ElementAtOrDefault(i);
                db.Add(new TournamentTeamMatchResult
                {
                    Match = match, TournamentId = tournament.Id, TeamId = teamIds[i], GoalsWon = score,
                    GoalsLost = request.Scores.Where((_, index) => index != i).DefaultIfEmpty(0).Max()
                });
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        if (!request.IsPlanned)
            await calculator.RecalculateAsync(tournament.Id, cancellationToken);
        await publisher.Publish(new MatchRecorded(tournament.Id, match.Id), cancellationToken);
        return match.Id;
    }

    private async Task<TournamentPlayer> FindOrCreatePlayer(Tournament tournament, string rawInitials, CancellationToken cancellationToken)
    {
        var initials = rawInitials.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == initials, cancellationToken);
        if (user is null)
        {
            user = new User { Username = initials, Name = initials };
            db.Users.Add(user);
        }
        var player = await db.TournamentPlayers.SingleOrDefaultAsync(x => x.TournamentId == tournament.Id && x.UserId == user.Id, cancellationToken);
        if (player is not null) return player;
        player = new TournamentPlayer { User = user, Tournament = tournament, Lives = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : null };
        db.Add(player);
        await db.SaveChangesAsync(cancellationToken);
        return player;
    }

    private TournamentTeam CreateTeam(Tournament tournament, List<TournamentPlayer> players, int number)
    {
        var team = new TournamentTeam { TournamentId = tournament.Id, Number = number, Name = $"Team {number}" };
        foreach (var player in players)
            team.Players.Add(new TournamentTeamPlayer { TournamentPlayer = player });
        db.Add(team);
        return team;
    }
}

public record PlanMatchesCommand(Guid TournamentId, int GamesPerPlayer, bool FixedTeams, SeedingType SeedingType, Guid? SeedTournamentId) : IRequest<int>;
public class PlanMatchesHandler(IdaslettenDbContext db, IPublisher publisher) : IRequestHandler<PlanMatchesCommand, int>
{
    public async Task<int> Handle(PlanMatchesCommand request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.Include(x => x.Players).SingleAsync(x => x.Id == request.TournamentId, cancellationToken);
        if (tournament.ParentTournamentId is not null && request.SeedTournamentId is not null)
            throw new InvalidOperationException("A round cannot be seeded from a separate tournament.");
        var players = tournament.Players.OrderByDescending(x => x.Score).ToList();
        if (request.SeedTournamentId is { } seedId)
            players = await db.TournamentPlayers.Where(x => x.TournamentId == seedId).OrderByDescending(x => x.Score)
                .Select(x => x.UserId).Join(db.TournamentPlayers.Where(x => x.TournamentId == tournament.Id), id => id, x => x.UserId, (_, x) => x).ToListAsync(cancellationToken);
        if (players.Count < tournament.TeamSize * 2) throw new InvalidOperationException("Add enough players to form two teams first.");
        tournament.SeedTournamentId ??= request.SeedTournamentId;
        var matchCount = Math.Max(1, (int)Math.Ceiling((double)players.Count * request.GamesPerPlayer / (tournament.TeamSize * 2)));
        var pairs = BuildTeams(players, tournament.TeamSize, request.SeedingType);
        var nextTeamNumber = (await db.TournamentTeams.Where(x => x.TournamentId == tournament.Id)
            .MaxAsync(x => (int?)x.Number, cancellationToken) ?? 0) + 1;
        for (var i = 0; i < matchCount; i++)
        {
            var current = request.FixedTeams ? pairs : Rotate(pairs, i);
            var match = new TournamentMatch { TournamentId = tournament.Id, State = MatchState.Planned, Order = (await db.TournamentMatches.Where(x => x.TournamentId == tournament.Id).MaxAsync(x => (int?)x.Order, cancellationToken) ?? 0) + 1 };
            db.Add(match);
            for (var t = 0; t < 2; t++)
            {
                var number = nextTeamNumber++;
                var team = new TournamentTeam { TournamentId = tournament.Id, Number = number, Name = $"Team {number}" };
                foreach (var player in current[t])
                    team.Players.Add(new TournamentTeamPlayer { TournamentPlayerId = player.Id });
                db.Add(new TournamentMatchTeam { Match = match, Team = team });
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        await publisher.Publish(new MatchesPlanned(tournament.Id, matchCount), cancellationToken);
        return matchCount;
    }

    private static List<List<TournamentPlayer>> BuildTeams(List<TournamentPlayer> players, int teamSize, SeedingType type)
    {
        var ordered = type switch
        {
            SeedingType.Random => players.OrderBy(_ => Random.Shared.Next()).ToList(),
            SeedingType.Equality => players.Select((x, i) => new { x, i }).OrderBy(x => x.i % 2 == 0 ? x.i / 2 : players.Count - 1 - x.i / 2).Select(x => x.x).ToList(),
            SeedingType.Fair => players.Take((players.Count + 1) / 2).Zip(players.Skip((players.Count + 1) / 2), (a, b) => new[] { a, b }).SelectMany(x => x).Concat(players.Skip(players.Count / 2 * 2)).ToList(),
            _ => players
        };
        return ordered.Chunk(teamSize).Where(x => x.Length == teamSize).Select(x => x.ToList()).ToList();
    }

    private static List<List<TournamentPlayer>> Rotate(List<List<TournamentPlayer>> teams, int round) =>
        teams.Skip(round % teams.Count).Concat(teams.Take(round % teams.Count)).ToList();
}
