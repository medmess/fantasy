using GfnTvBackend.Models;

namespace GfnTvBackend.Services;

public sealed class InMemoryNewsAdRepository : INewsAdRepository
{
    private readonly object _lock = new();
    private readonly List<NewsAd> _ads = [];

    public Task<IReadOnlyList<NewsAd>> GetActiveAsync()
    {
        lock (_lock)
        {
            var activeAds = _ads
                .Where(ad => ad.IsActive)
                .OrderByDescending(ad => ad.CreatedAt)
                .ToArray();
            return Task.FromResult<IReadOnlyList<NewsAd>>(activeAds);
        }
    }

    public Task<NewsAd> CreateAsync(NewsAd ad)
    {
        lock (_lock)
        {
            _ads.Add(ad);
            return Task.FromResult(ad);
        }
    }
}
