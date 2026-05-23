using GfnTvBackend.Models;

namespace GfnTvBackend.Services;

public sealed class FantasyScoringService
{
    private readonly FantasyScoringRules _rules = new();

    public FantasyRoundScore CalculateRound(FantasyRoundCalculationRequest request)
    {
        var playerScores = request.Starters
            .Take(11)
            .Where(player => request.StatsByPlayerId.ContainsKey(player.Id))
            .Select(player => CalculatePlayer(
                player,
                request.StatsByPlayerId[player.Id],
                player.Id == request.CaptainId,
                player.Id == request.ViceCaptainId))
            .ToArray();

        return new FantasyRoundScore(
            request.RoundNumber,
            $"Round {request.RoundNumber}",
            playerScores,
            playerScores.Sum(score => score.FinalPoints),
            DateTimeOffset.UtcNow);
    }

    private PlayerFantasyScore CalculatePlayer(
        FantasyPlayer player,
        PlayerMatchStats stats,
        bool isCaptain,
        bool isViceCaptain)
    {
        var items = new List<ScoreBreakdownItem>();

        void Add(string label, int points)
        {
            if (points != 0) items.Add(new ScoreBreakdownItem(label, points));
        }

        if (stats.Minutes > 0) Add("Played", _rules.Appearance);
        if (stats.Minutes >= 60) Add("60+ minutes", _rules.Played60Minutes);

        if (stats.Goals > 0)
        {
            var goalPoints = player.Position switch
            {
                "GK" or "DEF" => _rules.GoalkeeperDefenderGoal,
                "MID" => _rules.MidfielderGoal,
                _ => _rules.ForwardGoal
            };
            Add($"Goals x{stats.Goals}", goalPoints * stats.Goals);
        }

        Add($"Assists x{stats.Assists}", _rules.Assist * stats.Assists);

        if (stats.CleanSheet)
        {
            if (player.Position is "GK" or "DEF")
            {
                Add("Clean sheet", _rules.GoalkeeperDefenderCleanSheet);
            }
            else if (player.Position == "MID")
            {
                Add("Mid clean sheet", _rules.MidfielderCleanSheet);
            }
        }

        if (player.Position is "GK" or "DEF" && stats.GoalsConceded >= 2)
        {
            Add("Goals conceded", stats.GoalsConceded / 2 * _rules.TwoGoalsConceded);
        }

        if (player.Position == "GK")
        {
            Add("Saves", stats.Saves / 3 * _rules.ThreeSaves);
            Add("Penalty save", stats.PenaltiesSaved * _rules.PenaltySave);
        }

        Add("Yellow card", stats.YellowCards * _rules.YellowCard);
        Add("Red card", stats.RedCards * _rules.RedCard);
        Add("Own goal", stats.OwnGoals * _rules.OwnGoal);
        Add("Penalty miss", stats.PenaltiesMissed * _rules.PenaltyMiss);

        var basePoints = items.Sum(item => item.Points);
        var multiplier = isCaptain
            ? _rules.CaptainMultiplier
            : isViceCaptain
                ? _rules.ViceCaptainMultiplier
                : 1.0;
        var finalPoints = (int)Math.Round(basePoints * multiplier, MidpointRounding.AwayFromZero);

        if (isCaptain) Add($"Captain x{_rules.CaptainMultiplier:0}", finalPoints - basePoints);
        if (isViceCaptain) Add($"Vice x{_rules.ViceCaptainMultiplier:0.0}", finalPoints - basePoints);

        return new PlayerFantasyScore(
            player,
            stats,
            basePoints,
            finalPoints,
            isCaptain,
            isViceCaptain,
            items);
    }
}
