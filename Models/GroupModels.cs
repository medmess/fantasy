namespace GfnTvBackend.Models;

public sealed record AuthenticatedUser(string Id, string? Email);

public sealed record CreateGroupRequest(string Name, int MaxMembers = 7);

public sealed record JoinGroupRequest(string Code);

public sealed record FantasyGroup(
    string Id,
    string Code,
    string Name,
    string OwnerUserId,
    int Members,
    int MaxMembers,
    DateTimeOffset CreatedAt);
