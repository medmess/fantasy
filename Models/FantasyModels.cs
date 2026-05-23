namespace GfnTvBackend.Models;

public sealed record FantasyRoundCalculationRequest(
    int RoundNumber,
    IReadOnlyList<FantasyPlayer> Starters,
    string? CaptainId,
    string? ViceCaptainId,
    IReadOnlyDictionary<string, PlayerMatchStats> StatsByPlayerId);

public sealed record FantasyPlayer(
    string Id,
    string Name,
    string Team,
    string Position,
    decimal Price);

public sealed record PlayerMatchStats(
    int Minutes = 0,
    int Goals = 0,
    int Assists = 0,
    bool CleanSheet = false,
    int GoalsConceded = 0,
    int Saves = 0,
    int PenaltiesSaved = 0,
    int PenaltiesMissed = 0,
    int YellowCards = 0,
    int RedCards = 0,
    int OwnGoals = 0);

public sealed record FantasyRoundScore(
    int RoundNumber,
    string Label,
    IReadOnlyList<PlayerFantasyScore> PlayerScores,
    int TotalPoints,
    DateTimeOffset CalculatedAt);

public sealed record PlayerFantasyScore(
    FantasyPlayer Player,
    PlayerMatchStats Stats,
    int BasePoints,
    int FinalPoints,
    bool IsCaptain,
    bool IsViceCaptain,
    IReadOnlyList<ScoreBreakdownItem> Breakdown);

public sealed record ScoreBreakdownItem(string Label, int Points);

public sealed record FantasyScoringRules
{
    public int Appearance { get; init; } = 1;
    public int Played60Minutes { get; init; } = 1;
    public int GoalkeeperDefenderGoal { get; init; } = 6;
    public int MidfielderGoal { get; init; } = 5;
    public int ForwardGoal { get; init; } = 4;
    public int Assist { get; init; } = 3;
    public int GoalkeeperDefenderCleanSheet { get; init; } = 4;
    public int MidfielderCleanSheet { get; init; } = 1;
    public int TwoGoalsConceded { get; init; } = -1;
    public int ThreeSaves { get; init; } = 1;
    public int PenaltySave { get; init; } = 5;
    public int PenaltyMiss { get; init; } = -2;
    public int YellowCard { get; init; } = -1;
    public int RedCard { get; init; } = -3;
    public int OwnGoal { get; init; } = -2;
    public double CaptainMultiplier { get; init; } = 2.0;
    public double ViceCaptainMultiplier { get; init; } = 1.5;
}
