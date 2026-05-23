using GfnTvBackend.Models;

namespace GfnTvBackend.Services;

public sealed class StandingsService
{
    public IReadOnlyList<GroupStandingResult> Calculate(StandingsCalculationRequest request)
    {
        var rows = request.Teams.ToDictionary(
            team => team.Code,
            team => new MutableStanding(team));

        foreach (var result in request.Results)
        {
            if (!rows.TryGetValue(result.HomeTeamCode, out var home) ||
                !rows.TryGetValue(result.AwayTeamCode, out var away))
            {
                continue;
            }

            home.Played++;
            away.Played++;
            home.GoalsFor += result.HomeGoals;
            home.GoalsAgainst += result.AwayGoals;
            away.GoalsFor += result.AwayGoals;
            away.GoalsAgainst += result.HomeGoals;
            home.FairPlayPenalty += result.HomeFairPlayPenalty;
            away.FairPlayPenalty += result.AwayFairPlayPenalty;

            if (result.HomeGoals > result.AwayGoals)
            {
                home.Wins++;
                away.Losses++;
            }
            else if (result.HomeGoals < result.AwayGoals)
            {
                away.Wins++;
                home.Losses++;
            }
            else
            {
                home.Draws++;
                away.Draws++;
            }
        }

        return rows.Values
            .GroupBy(row => row.Team.Group)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var ranked = group
                    .OrderByDescending(row => row.Points)
                    .ThenByDescending(row => row.GoalDifference)
                    .ThenByDescending(row => row.GoalsFor)
                    .ThenBy(row => row.FairPlayPenalty)
                    .ThenBy(row => row.Team.Name)
                    .Select((row, index) => row.ToResult(
                        index + 1,
                        index < 2
                            ? "qualified"
                            : index == 2
                                ? "third-place-race"
                                : "eliminated"))
                    .ToArray();

                return new GroupStandingResult(group.Key, ranked);
            })
            .ToArray();
    }

    private sealed class MutableStanding(TeamSeed team)
    {
        public TeamSeed Team { get; } = team;
        public int Played { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int FairPlayPenalty { get; set; }
        public int GoalDifference => GoalsFor - GoalsAgainst;
        public int Points => Wins * 3 + Draws;

        public StandingRow ToResult(int rank, string status)
        {
            return new StandingRow(
                rank,
                Team,
                Played,
                Wins,
                Draws,
                Losses,
                GoalsFor,
                GoalsAgainst,
                GoalDifference,
                Points,
                FairPlayPenalty,
                status);
        }
    }
}
