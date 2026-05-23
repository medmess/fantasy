using GfnTvBackend.Models;

namespace GfnTvBackend.Services;

public sealed class NewsService(INewsRepository repository)
{
    public async Task<NewsPost?> AddTelegramPostAsync(TelegramNewsPostRequest request)
    {
        var existing = await repository.FindByTelegramPostIdAsync(request.TelegramPostId);
        if (existing is not null) return null;

        var post = new NewsPost(
            Guid.NewGuid().ToString("N"),
            request.TelegramPostId,
            request.Caption.Trim(),
            request.ImagePath.Trim(),
            null,
            string.IsNullOrWhiteSpace(request.Source) ? "Offside" : request.Source.Trim(),
            request.PublishedAt,
            DateTimeOffset.UtcNow);

        return await repository.CreateAsync(post);
    }

    public Task<NewsPost> AddAdminPostAsync(AdminNewsPostRequest request)
    {
        var imageUrl = request.ImageUrl.Trim();
        var post = new NewsPost(
            Guid.NewGuid().ToString("N"),
            -DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            request.Caption.Trim(),
            imageUrl,
            imageUrl,
            string.IsNullOrWhiteSpace(request.Source) ? "GFN Admin" : request.Source.Trim(),
            request.PublishedAt ?? DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        return repository.CreateAsync(post);
    }

    public Task<IReadOnlyList<NewsPost>> GetLatestAsync(int limit)
    {
        return repository.GetLatestAsync(Math.Clamp(limit, 1, 50));
    }

    public Task<bool> DeleteByTelegramPostIdAsync(long telegramPostId)
    {
        return repository.DeleteByTelegramPostIdAsync(telegramPostId);
    }
}

public interface INewsRepository
{
    Task<NewsPost?> FindByTelegramPostIdAsync(long telegramPostId);
    Task<NewsPost> CreateAsync(NewsPost post);
    Task<IReadOnlyList<NewsPost>> GetLatestAsync(int limit);
    Task<bool> DeleteByTelegramPostIdAsync(long telegramPostId);
}
