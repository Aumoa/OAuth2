using OAuth2.Options;
using OAuth2.Services;

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
    s.AddHostedService<RedisConnection>();

    var backendUrl = config["OAuth2:BackendUrl"] ?? throw new InvalidOperationException("OAuth2:BackendUrl is not configured.");
    s.AddHttpClient<IBackendClient, HttpBackendClient>(client => client.BaseAddress = new Uri(backendUrl));
    return s;
}