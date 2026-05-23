using GfnTvBackend.Models;
using GfnTvBackend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FlutterDev", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

builder.Services.Configure<SupabaseOptions>(
    builder.Configuration.GetSection("Supabase"));
builder.Services.AddHttpClient<SupabaseAuthService>();
builder.Services.AddHttpClient<SupabaseGroupRepository>();
builder.Services.AddHttpClient<SupabaseNewsRepository>();
builder.Services.AddHttpClient<SupabaseStorageService>();
builder.Services.AddSingleton<InMemoryGroupRepository>();
builder.Services.AddSingleton<InMemoryNewsRepository>();
builder.Services.AddSingleton<GroupCodeGenerator>();
builder.Services.AddScoped<FantasyScoringService>();
builder.Services.AddScoped<StandingsService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<NewsService>();
builder.Services.AddScoped<IGroupRepository>(provider =>
{
    var options = provider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<SupabaseOptions>>()
        .Value;

    return options.IsConfigured
        ? provider.GetRequiredService<SupabaseGroupRepository>()
        : provider.GetRequiredService<InMemoryGroupRepository>();
});
builder.Services.AddScoped<INewsRepository>(provider =>
{
    var options = provider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<SupabaseOptions>>()
        .Value;

    return options.IsConfigured
        ? provider.GetRequiredService<SupabaseNewsRepository>()
        : provider.GetRequiredService<InMemoryNewsRepository>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("FlutterDev");
app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "GFN.TV backend",
    mode = app.Configuration.GetSection("Supabase").Get<SupabaseOptions>()?.IsConfigured == true
        ? "supabase"
        : "memory"
}));

app.MapPost("/api/fantasy/calculate-points",
    (FantasyRoundCalculationRequest request, FantasyScoringService scoring) =>
    {
        var result = scoring.CalculateRound(request);
        return Results.Ok(result);
    });

app.MapPost("/api/standings/calculate",
    (StandingsCalculationRequest request, StandingsService standings) =>
    {
        var result = standings.Calculate(request);
        return Results.Ok(result);
    });

app.MapPost("/api/news/telegram", async (
    TelegramNewsPostRequest request,
    NewsService news) =>
{
    if (request.TelegramPostId <= 0 ||
        string.IsNullOrWhiteSpace(request.Caption) ||
        string.IsNullOrWhiteSpace(request.ImagePath))
    {
        return Results.BadRequest(new { message = "telegramPostId, caption and imagePath are required." });
    }

    var post = await news.AddTelegramPostAsync(request);
    return Results.Ok(NewsPostResponse.From(post!));
});

app.MapPost("/api/news/admin", async (
    AdminNewsPostRequest request,
    NewsService news) =>
{
    if (string.IsNullOrWhiteSpace(request.Caption) ||
        string.IsNullOrWhiteSpace(request.ImageUrl))
    {
        return Results.BadRequest(new { message = "caption and imageUrl are required." });
    }

    var post = await news.AddAdminPostAsync(request);
    return Results.Ok(NewsPostResponse.From(post));
});

app.MapGet("/api/news/latest", async (int? limit, NewsService news, HttpRequest request) =>
{
    var posts = await news.GetLatestAsync(limit ?? 30);
    var forwardedHost = request.Headers["X-Forwarded-Host"].FirstOrDefault();
    var forwardedProto = request.Headers["X-Forwarded-Proto"].FirstOrDefault();
    var publicBaseUrl = request.Headers["X-Public-Base-Url"].FirstOrDefault();
    var baseUrl = !string.IsNullOrWhiteSpace(publicBaseUrl)
        ? publicBaseUrl.TrimEnd('/')
        : $"{(string.IsNullOrWhiteSpace(forwardedProto) ? request.Scheme : forwardedProto)}://{(string.IsNullOrWhiteSpace(forwardedHost) ? request.Host : forwardedHost)}";
    return Results.Ok(posts.Select(post => NewsPostResponse.From(post, baseUrl)));
});

app.MapDelete("/api/news/telegram/{telegramPostId:long}", async (
    long telegramPostId,
    NewsService news) =>
{
    var deleted = await news.DeleteByTelegramPostIdAsync(telegramPostId);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/api/news/image/{fileName}", (string fileName) =>
{
    var safeName = Path.GetFileName(fileName);
    var downloads = Path.Combine(@"C:\telegram-news", "downloads");
    var fullPath = Path.GetFullPath(Path.Combine(downloads, safeName));
    var downloadsRoot = Path.GetFullPath(downloads);

    if (!fullPath.StartsWith(downloadsRoot, StringComparison.OrdinalIgnoreCase) ||
        !File.Exists(fullPath))
    {
        return Results.NotFound();
    }

    return Results.File(fullPath, contentType: "image/jpeg");
});

app.MapPost("/api/groups/create", async (
    HttpContext httpContext,
    CreateGroupRequest request,
    SupabaseAuthService auth,
    GroupService groups) =>
{
    var user = await auth.GetUserAsync(httpContext);
    if (user is null) return Results.Unauthorized();

    var group = await groups.CreateGroupAsync(user, request);
    return Results.Ok(group);
});

app.MapPost("/api/groups/join", async (
    HttpContext httpContext,
    JoinGroupRequest request,
    SupabaseAuthService auth,
    GroupService groups) =>
{
    var user = await auth.GetUserAsync(httpContext);
    if (user is null) return Results.Unauthorized();

    var group = await groups.JoinGroupAsync(user, request.Code);
    return group is null
        ? Results.NotFound(new { message = "Group not found or full." })
        : Results.Ok(group);
});

app.MapGet("/api/groups/mine", async (
    HttpContext httpContext,
    SupabaseAuthService auth,
    GroupService groups) =>
{
    var user = await auth.GetUserAsync(httpContext);
    if (user is null) return Results.Unauthorized();

    return Results.Ok(await groups.GetMineAsync(user));
});

app.Run();

sealed record NewsPostResponse(
    string Id,
    long TelegramPostId,
    string Caption,
    string ImagePath,
    string ImageUrl,
    string Source,
    DateTimeOffset PublishedAt,
    DateTimeOffset CreatedAt)
{
    public static NewsPostResponse From(NewsPost post, string? baseUrl = null)
    {
        var imageUrl = post.ImageUrl;
        if (string.IsNullOrWhiteSpace(imageUrl) && !string.IsNullOrWhiteSpace(baseUrl))
        {
            var fileName = Path.GetFileName(post.ImagePath);
            imageUrl = $"{baseUrl}/api/news/image/{Uri.EscapeDataString(fileName)}";
        }

        return new NewsPostResponse(
            post.Id,
            post.TelegramPostId,
            post.Caption,
            post.ImagePath,
            imageUrl ?? post.ImagePath,
            post.Source,
            post.PublishedAt,
            post.CreatedAt);
    }
}
