using Idasletten.Features.Matches.Queries;

namespace Idasletten.Pages.Shared;

/// <summary>View model for the _MatchLink partial - one clickable match line.</summary>
public record MatchLinkModel(Guid TournamentId, MatchRow Match, bool ShowTime = false);
