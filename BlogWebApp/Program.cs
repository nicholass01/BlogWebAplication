using System.Security.Claims;
using Blog.Application;
using Blog.Application.Contracts.Auth;
using Blog.Application.Contracts.Posts;
using Blog.Application.Services;
using Blog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// OpenAPI document (json). Swagger UI não está incluído.
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4173",
                "http://localhost:5173",
                "https://localhost:7047",
                "http://localhost:7047")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("frontend");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Health/root endpoint
app.MapGet("/", () => Results.Ok(new { status = "ok", message = "Blog API online" }));

// Posts endpoints
var posts = app.MapGroup("/api/posts");

posts.MapGet("/", async (IPostService postService, CancellationToken ct) =>
{
    var result = await postService.ListPublishedAsync(ct);
    return Results.Ok(result);
});

posts.MapGet("/{slug}", async (string slug, IPostService postService, CancellationToken ct) =>
{
    var post = await postService.GetBySlugAsync(slug, ct);
    return post is not null ? Results.Ok(post) : Results.NotFound();
});

posts.MapPost("/", async (CreatePostRequest request, ClaimsPrincipal user, IPostService postService, CancellationToken ct) =>
{
    var authorId = GetUserId(user);
    if (authorId == Guid.Empty)
    {
        return Results.Unauthorized();
    }

    var enrichedRequest = request with { AuthorId = authorId };
    var id = await postService.CreateAsync(enrichedRequest, ct);
    return Results.Created($"/api/posts/{id}", new { id });
}).RequireAuthorization();

posts.MapPut("/{id:guid}", async (Guid id, UpdatePostRequest request, ClaimsPrincipal user, IPostService postService, CancellationToken ct) =>
{
    var authorId = GetUserId(user);
    if (authorId == Guid.Empty)
    {
        return Results.Unauthorized();
    }

    var enrichedRequest = request with { PostId = id, AuthorId = authorId };
    await postService.UpdateAsync(enrichedRequest, ct);
    return Results.NoContent();
}).RequireAuthorization();

posts.MapPost("/{id:guid}/publish", async (Guid id, IPostService postService, CancellationToken ct) =>
{
    await postService.PublishAsync(id, ct);
    return Results.NoContent();
}).RequireAuthorization();

posts.MapDelete("/{id:guid}", async (Guid id, IPostService postService, CancellationToken ct) =>
{
    await postService.DeleteAsync(id, ct);
    return Results.NoContent();
}).RequireAuthorization();

// Auth endpoints
var auth = app.MapGroup("/api/auth");

auth.MapPost("/register", async (RegisterRequest request, IAuthService authService, CancellationToken ct) =>
{
    var result = await authService.RegisterAsync(request, ct);
    return Results.Ok(result);
});

auth.MapPost("/login", async (LoginRequest request, IAuthService authService, CancellationToken ct) =>
{
    var result = await authService.LoginAsync(request, ct);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
});

app.Run();

static Guid GetUserId(ClaimsPrincipal user)
{
    var idValue = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    return Guid.TryParse(idValue, out var id) ? id : Guid.Empty;
}
