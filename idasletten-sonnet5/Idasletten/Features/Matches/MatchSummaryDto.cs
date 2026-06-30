namespace Idasletten.Features.Matches;

public record MatchSummaryDto(Guid Id, int Order, MatchState State, string Label, string? ScoreLabel);
