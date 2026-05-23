using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GfnTvBackend.Models;
using Microsoft.Extensions.Options;

namespace GfnTvBackend.Services;

public sealed class SupabaseStorageService(
    HttpClient httpClient,
    IOptions<SupabaseOptions> options,
    ILogger<SupabaseStorageService> logger)
{
    private const string BucketName = "news-images";
    private readonly SupabaseOptions _options = options.Value;
    private bool _bucketChecked;

    public async Task<string?> UploadNewsImageAsync(
        long telegramPostId,
        string? imageBase64,
        string? contentType)
    {
        if (!_options.IsConfigured || string.IsNullOrWhiteSpace(imageBase64))
        {
            return null;
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(imageBase64);
        }
        catch (FormatException)
        {
            logger.LogWarning("Invalid imageBase64 for Telegram post {PostId}", telegramPostId);
            return null;
        }

        if (bytes.Length == 0) return null;

        await EnsureBucketAsync();

        var safeContentType = string.IsNullOrWhiteSpace(contentType)
            ? "image/jpeg"
            : contentType.Trim();
        var extension = safeContentType.Contains("png", StringComparison.OrdinalIgnoreCase)
            ? "png"
            : safeContentType.Contains("webp", StringComparison.OrdinalIgnoreCase)
                ? "webp"
                : "jpg";
        var objectPath = $"telegram/{telegramPostId}.{extension}";

        using var request = CreateRequest(
            HttpMethod.Post,
            $"storage/v1/object/{BucketName}/{objectPath}");
        request.Headers.TryAddWithoutValidation("x-upsert", "true");
        request.Content = new ByteArrayContent(bytes);
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(safeContentType);

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return $"{_options.Url!.TrimEnd('/')}/storage/v1/object/public/{BucketName}/{Uri.EscapeDataString(objectPath).Replace("%2F", "/")}";
    }

    private async Task EnsureBucketAsync()
    {
        if (_bucketChecked) return;

        using var request = CreateRequest(HttpMethod.Post, "storage/v1/bucket");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                id = BucketName,
                name = BucketName,
                @public = true
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode ||
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
        {
            _bucketChecked = true;
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(
            method,
            $"{_options.Url!.TrimEnd('/')}/{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _options.ServiceRoleKey);
        request.Headers.Add("apikey", _options.ServiceRoleKey);
        return request;
    }
}
