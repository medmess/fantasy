namespace GfnTvBackend.Models;

public sealed record TelegramNewsPostRequest(
    long TelegramPostId,
    string Caption,
    string ImagePath,
    DateTimeOffset PublishedAt,
    string Source);

public sealed record AdminNewsPostRequest(
    string Caption,
    string ImageUrl,
    DateTimeOffset? PublishedAt,
    string? Source);

public sealed record NewsPost(
    string Id,
    long TelegramPostId,
    string Caption,
    string ImagePath,
    string? ImageUrl,
    string Source,
    DateTimeOffset PublishedAt,
    DateTimeOffset CreatedAt);
