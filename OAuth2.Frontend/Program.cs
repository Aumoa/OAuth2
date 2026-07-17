using Microsoft.AspNetCore.StaticFiles;
using OAuth2.Options;
using OAuth2.OpenId;
using OAuth2.Services;
using BffSessionOptions = OAuth2.Options.SessionOptions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
Configure(builder.Services, builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseDefaultFiles();
app.UseStaticFiles(CreateStaticFileOptions());

app.UseAuthorization();

app.MapControllers();
MapSpaFallback(app);

app.Run();

static StaticFileOptions CreateStaticFileOptions()
{
    return new StaticFileOptions
    {
        OnPrepareResponse = static context =>
        {
            var path = context.Context.Request.Path;
            if (path.StartsWithSegments("/assets"))
            {
                context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
            }
            else if (path.Equals("/index.html"))
            {
                context.Context.Response.Headers.CacheControl = "no-cache";
            }
        }
    };
}

static void MapSpaFallback(WebApplication app)
{
    PathString[] serverPaths =
    [
        "/api",
        "/.well-known",
        "/authorize",
        "/token",
        "/revoke",
        "/userinfo",
        "/jwks"
    ];

    app.MapFallback(async context =>
    {
        var request = context.Request;
        if ((!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
            || serverPaths.Any(request.Path.StartsWithSegments))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var indexFile = app.Environment.WebRootFileProvider.GetFileInfo("index.html");
        if (!indexFile.Exists)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-cache";
        await context.Response.SendFileAsync(indexFile, context.RequestAborted);
    });
}

static IServiceCollection Configure(IServiceCollection s, IConfiguration config)
{
    s.Configure<RedisOptions>(config.GetRequiredSection(nameof(RedisOptions)));
    s.AddOptions<OAuthOptions>()
        .Bind(config.GetRequiredSection("OAuth2"))
        .Validate(static options => Uri.TryCreate(options.BackendUrl, UriKind.Absolute, out _), "OAuth2:BackendUrl must be an absolute URI.")
        .Validate(static options => !string.IsNullOrWhiteSpace(options.ClientId), "OAuth2:ClientId is required.")
        .ValidateOnStart();
    s.AddOptions<OidcProviderOptions>()
        .Bind(config.GetRequiredSection("OpenId"))
        .Validate(
            static options => OidcEndpointUris.TryNormalizeIssuer(options.Issuer, out _),
            "OpenId:Issuer must be an HTTPS origin (HTTP is allowed only for loopback).")
        .ValidateOnStart();
    s.AddOptions<BffSessionOptions>()
        .Bind(config.GetRequiredSection("SessionOptions"))
        .Validate(static options => !string.IsNullOrWhiteSpace(options.CookieName), "SessionOptions:CookieName is required.")
        .Validate(static options => options.CookieName.StartsWith("__Host-", StringComparison.Ordinal), "SessionOptions:CookieName must use the __Host- prefix.")
        .Validate(static options => options.LifetimeMinutes > 0, "SessionOptions:LifetimeMinutes must be positive.")
        .Validate(static options => options.BrowserLifetimeDays > 0, "SessionOptions:BrowserLifetimeDays must be positive.")
        .Validate(static options => options.BrowserLifetime > options.Lifetime, "SessionOptions:BrowserLifetimeDays must exceed the active session lifetime.")
        .Validate(static options => options.MaxRememberedAccounts > 0, "SessionOptions:MaxRememberedAccounts must be positive.")
        .ValidateOnStart();

    s.AddSingleton<RedisConnection>();
    s.AddHostedService(services => services.GetRequiredService<RedisConnection>());
    s.AddScoped<ISessionsRepository, RedisSessionsRepository>();
    s.AddScoped<BrowserSessionSignIn>();

    var backendUrl = config["OAuth2:BackendUrl"] ?? throw new InvalidOperationException("OAuth2:BackendUrl is not configured.");
    s.AddHttpClient<IBackendClient, HttpBackendClient>(client => client.BaseAddress = new Uri(backendUrl));
    return s;
}
