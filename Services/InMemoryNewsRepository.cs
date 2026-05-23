using GfnTvBackend.Models;

namespace GfnTvBackend.Services;

public sealed class InMemoryNewsRepository : INewsRepository
{
    private readonly object _lock = new();
    private readonly Dictionary<long, NewsPost> _postsByTelegramId = [];

    public Task<NewsPost?> FindByTelegramPostIdAsync(long telegramPostId)
    {
        lock (_lock)
        {
            _postsByTelegramId.TryGetValue(telegramPostId, out var post);
            return Task.FromResult(post);
        }
    }

    public Task<NewsPost> CreateAsync(NewsPost post)
    {
        lock (_lock)
        {
            _postsByTelegramId[post.TelegramPostId] = post;
            return Task.FromResult(post);
        }
    }

    public Task<IReadOnlyList<NewsPost>> GetLatestAsync(int limit)
    {
        lock (_lock)
        {
            var posts = _postsByTelegramId.Values
                .OrderByDescending(post => post.PublishedAt)
                .Take(limit)
                .ToArray();
            return Task.FromResult<IReadOnlyList<NewsPost>>(posts);
        }
    }

    public Task<bool> DeleteByTelegramPostIdAsync(long telegramPostId)
    {
        lock (_lock)
        {
            return Task.FromResult(_postsByTelegramId.Remove(telegramPostId));
        }
    }
}
