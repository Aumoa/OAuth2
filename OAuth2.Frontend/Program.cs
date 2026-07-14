using OAuth2.Options;
using OAuth2.Services;
using BffSessionOptions = OAuth2.Options.SessionOptions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
Configure(builder.Services, builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

static IServiceCollection Configure(IServiceCollection s, IConfiguration config)
{
    s.Configure<RedisOptions>(config.GetRequiredSection(nameof(RedisOptions)));
    s.AddOptions<OAuthOptions>()
        .Bind(config.GetRequiredSection("OAuth2"))
        .Validate(static options => Uri.TryCreate(options.BackendUrl, UriKind.Absolute, out _), "OAuth2:BackendUrl must be an absolute URI.")
        .Validate(static options => !string.IsNullOrWhiteSpace(options.ClientId), "OAuth2:ClientId is required.")
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

    var backendUrl = config["OAuth2:BackendUrl"] ?? throw new InvalidOperationException("OAuth2:BackendUrl is not configured.");
    s.AddHttpClient<IBackendClient, HttpBackendClient>(client => client.BaseAddress = new Uri(backendUrl));
    return s;
}
