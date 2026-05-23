namespace GfnTvBackend.Models;

public sealed record SupabaseOptions
{
    public string? Url { get; init; }
    public string? ServiceRoleKey { get; init; }
    public string? AnonKey { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url) &&
        !string.IsNullOrWhiteSpace(ServiceRoleKey);
}
