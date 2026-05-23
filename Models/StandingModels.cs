namespace GfnTvBackend.Models;

public sealed record StandingsCalculationRequest(
    IReadOnlyList<TeamSeed> Teams,
    IReadOnlyList<MatchResult> Results);

public sealed record TeamSeed(string Name, string Code, string Group);

public sealed record MatchResult(
    string Group,
    string HomeTeamCode,
    string AwayTeamCode,
    int HomeGoals,
    int AwayGoals,
    int HomeFairPlayPenalty = 0,
    int AwayFairPlayPenalty = 0);

public sealed record GroupStandingResult(
    string Group,
    IReadOnlyList<StandingRow> Rows);

public sealed record StandingRow(
    int Rank,
    TeamSeed Team,
    int Played,
    int Wins,
    int Draws,
    int Losses,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points,
    int FairPlayPenalty,
    string QualificationStatus);
