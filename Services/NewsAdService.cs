using GfnTvBackend.Models;

namespace GfnTvBackend.Services;

public sealed class NewsAdService(INewsAdRepository repository)
{
    public Task<IReadOnlyList<NewsAd>> GetActiveAsync()
    {
        return repository.GetActiveAsync();
    }

    public Task<NewsAd> CreateAsync(NewsAdRequest request)
    {
        var ad = new NewsAd(
            Guid.NewGuid().ToString("N"),
            request.Title.Trim(),
            string.IsNullOrWhiteSpace(request.Subtitle) ? null : request.Subtitle.Trim(),
            request.ImageUrl.Trim(),
            string.IsNullOrWhiteSpace(request.TargetUrl) ? null : request.TargetUrl.Trim(),
            request.IsActive ?? true,
            DateTimeOffset.UtcNow);

        return repository.CreateAsync(ad);
    }
}

public interface INewsAdRepository
{
    Task<IReadOnlyList<NewsAd>> GetActiveAsync();
    Task<NewsAd> CreateAsync(NewsAd ad);
}
