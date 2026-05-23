using GfnTvBackend.Models;

namespace GfnTvBackend.Services;

public sealed class GroupService(
    IGroupRepository repository,
    GroupCodeGenerator codeGenerator)
{
    public async Task<FantasyGroup> CreateGroupAsync(
        AuthenticatedUser user,
        CreateGroupRequest request)
    {
        var group = new FantasyGroup(
            Guid.NewGuid().ToString("N"),
            codeGenerator.Create(),
            request.Name.Trim(),
            user.Id,
            Members: 1,
            MaxMembers: Math.Clamp(request.MaxMembers, 2, 50),
            CreatedAt: DateTimeOffset.UtcNow);

        await repository.CreateAsync(group, user);
        return group;
    }

    public Task<FantasyGroup?> JoinGroupAsync(AuthenticatedUser user, string code)
    {
        return repository.JoinAsync(user, code.Trim().ToUpperInvariant());
    }

    public Task<IReadOnlyList<FantasyGroup>> GetMineAsync(AuthenticatedUser user)
    {
        return repository.GetMineAsync(user);
    }
}

public sealed class GroupCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string Create()
    {
        return new string(Enumerable
            .Range(0, 6)
            .Select(_ => Alphabet[Random.Shared.Next(Alphabet.Length)])
            .ToArray());
    }
}

public interface IGroupRepository
{
    Task CreateAsync(FantasyGroup group, AuthenticatedUser user);
    Task<FantasyGroup?> JoinAsync(AuthenticatedUser user, string code);
    Task<IReadOnlyList<FantasyGroup>> GetMineAsync(AuthenticatedUser user);
}
