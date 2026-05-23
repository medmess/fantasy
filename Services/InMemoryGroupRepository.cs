using GfnTvBackend.Models;

namespace GfnTvBackend.Services;

public sealed class InMemoryGroupRepository : IGroupRepository
{
    private readonly object _lock = new();
    private readonly Dictionary<string, FantasyGroup> _groupsByCode = [];
    private readonly Dictionary<string, HashSet<string>> _membersByGroupId = [];

    public Task CreateAsync(FantasyGroup group, AuthenticatedUser user)
    {
        lock (_lock)
        {
            _groupsByCode[group.Code] = group;
            _membersByGroupId[group.Id] = [user.Id];
        }

        return Task.CompletedTask;
    }

    public Task<FantasyGroup?> JoinAsync(AuthenticatedUser user, string code)
    {
        lock (_lock)
        {
            if (!_groupsByCode.TryGetValue(code, out var group)) return Task.FromResult<FantasyGroup?>(null);

            var members = _membersByGroupId[group.Id];
            if (members.Count >= group.MaxMembers && !members.Contains(user.Id))
            {
                return Task.FromResult<FantasyGroup?>(null);
            }

            members.Add(user.Id);
            var updated = group with { Members = members.Count };
            _groupsByCode[code] = updated;
            return Task.FromResult<FantasyGroup?>(updated);
        }
    }

    public Task<IReadOnlyList<FantasyGroup>> GetMineAsync(AuthenticatedUser user)
    {
        lock (_lock)
        {
            var groups = _groupsByCode.Values
                .Where(group => _membersByGroupId.TryGetValue(group.Id, out var members) &&
                                members.Contains(user.Id))
                .OrderByDescending(group => group.CreatedAt)
                .ToArray();

            return Task.FromResult<IReadOnlyList<FantasyGroup>>(groups);
        }
    }
}
