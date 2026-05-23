using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GfnTvBackend.Models;
using Microsoft.Extensions.Options;

namespace GfnTvBackend.Services;

public sealed class SupabaseGroupRepository(
    HttpClient httpClient,
    IOptions<SupabaseOptions> options) : IGroupRepository
{
    private readonly SupabaseOptions _options = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task CreateAsync(FantasyGroup group, AuthenticatedUser user)
    {
        await SendAsync(HttpMethod.Post, "fantasy_groups", new
        {
            id = group.Id,
            code = group.Code,
            name = group.Name,
            owner_user_id = group.OwnerUserId,
            max_members = group.MaxMembers,
            created_at = group.CreatedAt
        });

        await SendAsync(HttpMethod.Post, "fantasy_group_members", new
        {
            group_id = group.Id,
            user_id = user.Id,
            joined_at = DateTimeOffset.UtcNow
        });
    }

    public async Task<FantasyGroup?> JoinAsync(AuthenticatedUser user, string code)
    {
        var group = await FindByCodeAsync(code);
        if (group is null || group.Members >= group.MaxMembers) return null;

        await SendAsync(HttpMethod.Post, "fantasy_group_members", new
        {
            group_id = group.Id,
            user_id = user.Id,
            joined_at = DateTimeOffset.UtcNow
        }, preferResolutionMergeDuplicates: true);

        var updated = group with { Members = group.Members + 1 };
        await SendAsync(
            new HttpMethod("PATCH"),
            $"fantasy_groups?id=eq.{group.Id}",
            new { members = updated.Members });

        return updated;
    }

    public async Task<IReadOnlyList<FantasyGroup>> GetMineAsync(AuthenticatedUser user)
    {
        var members = await GetJsonArrayAsync(
            $"fantasy_group_members?select=group_id&user_id=eq.{user.Id}");
        var groupIds = members
            .Select(item => item.GetProperty("group_id").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray();

        if (groupIds.Length == 0) return [];

        var idFilter = string.Join(",", groupIds);
        var groups = await GetJsonArrayAsync(
            $"fantasy_groups?select=*&id=in.({idFilter})&order=created_at.desc");

        return groups.Select(ParseGroup).ToArray();
    }

    private async Task<FantasyGroup?> FindByCodeAsync(string code)
    {
        var groups = await GetJsonArrayAsync($"fantasy_groups?select=*&code=eq.{code}");
        return groups.Count == 0 ? null : ParseGroup(groups[0]);
    }

    private FantasyGroup ParseGroup(JsonElement item)
    {
        return new FantasyGroup(
            item.GetProperty("id").GetString()!,
            item.GetProperty("code").GetString()!,
            item.GetProperty("name").GetString()!,
            item.GetProperty("owner_user_id").GetString()!,
            item.TryGetProperty("members", out var members) ? members.GetInt32() : 1,
            item.GetProperty("max_members").GetInt32(),
            item.GetProperty("created_at").GetDateTimeOffset());
    }

    private async Task<IReadOnlyList<JsonElement>> GetJsonArrayAsync(string path)
    {
        using var request = CreateRequest(HttpMethod.Get, path);
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.EnumerateArray().Select(item => item.Clone()).ToArray();
    }

    private async Task SendAsync(
        HttpMethod method,
        string path,
        object body,
        bool preferResolutionMergeDuplicates = false)
    {
        using var request = CreateRequest(method, path);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");

        request.Headers.TryAddWithoutValidation(
            "Prefer",
            preferResolutionMergeDuplicates
                ? "resolution=merge-duplicates,return=representation"
                : "return=representation");

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(
            method,
            $"{_options.Url!.TrimEnd('/')}/rest/v1/{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _options.ServiceRoleKey);
        request.Headers.Add("apikey", _options.ServiceRoleKey);
        return request;
    }
}
