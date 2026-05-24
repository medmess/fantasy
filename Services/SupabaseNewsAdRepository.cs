using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GfnTvBackend.Models;
using Microsoft.Extensions.Options;

namespace GfnTvBackend.Services;

public sealed class SupabaseNewsAdRepository(
    HttpClient httpClient,
    IOptions<SupabaseOptions> options) : INewsAdRepository
{
    private readonly SupabaseOptions _options = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<NewsAd>> GetActiveAsync()
    {
        using var request = CreateRequest(
            HttpMethod.Get,
            "news_ads?select=*&is_active=eq.true&order=created_at.desc");
        using var response = await httpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.EnumerateArray().Select(ParseAd).ToArray();
    }

    public async Task<NewsAd> CreateAsync(NewsAd ad)
    {
        using var request = CreateRequest(HttpMethod.Post, "news_ads");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                id = ad.Id,
                title = ad.Title,
                subtitle = ad.Subtitle,
                image_url = ad.ImageUrl,
                target_url = ad.TargetUrl,
                placement = "news_feed",
                is_active = ad.IsActive,
                created_at = ad.CreatedAt
            }, JsonOptions),
            Encoding.UTF8,
            "application/json");
        request.Headers.TryAddWithoutValidation("Prefer", "return=representation");

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return ad;
    }

    private static NewsAd ParseAd(JsonElement item)
    {
        return new NewsAd(
            item.GetProperty("id").GetString()!,
            item.GetProperty("title").GetString() ?? "",
            item.TryGetProperty("subtitle", out var subtitle) ? subtitle.GetString() : null,
            item.GetProperty("image_url").GetString() ?? "",
            item.TryGetProperty("target_url", out var targetUrl) ? targetUrl.GetString() : null,
            item.TryGetProperty("is_active", out var isActive) && isActive.GetBoolean(),
            item.GetProperty("created_at").GetDateTimeOffset());
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
