using Amazon;
using Amazon.SimpleEmail;
using Microsoft.Extensions.Options;
using OAuth2;
using OAuth2.Localizational;
using OAuth2.Options;
using OAuth2.Repositories;
using OAuth2.Services;
using OAuth2.SQL;
using SQLMigration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
Configure(builder.Services, builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

ValidateEmailLocalization(app.Services);

app.UseRequestLocalization();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    await StartMigrationAsync(app.Lifetime.ApplicationStopping);
}

app.Run();

return;

static IServiceCollection Configure(IServiceCollection s, IConfiguration config)
{
    s.Configure<MySqlOptions>(config.GetRequiredSection(nameof(MySqlOptions)));
    s.Configure<RedisOptions>(config.GetRequiredSection(nameof(RedisOptions)));
    s.AddOptions<OAuthOptions>()
        .Bind(config.GetRequiredSection("OAuth2"))
        .Validate(static options => !string.IsNullOrWhiteSpace(options.ClientId), "OAuth2:ClientId is required.")
        .ValidateOnStart();
    s.AddOptions<SESOptions>()
        .Bind(config.GetRequiredSection(nameof(SESOptions)))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // The marker type namespace matches the embedded resource base name:
    // OAuth2.Localizational.Strings.
    s.AddLocalization();
    s.Configure<RequestLocalizationOptions>(options =>
    {
        string[] supportedCultures = ["en", "ko", "ja"];
        options.SetDefaultCulture("en")
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);
    });

    s.AddSingleton<IAmazonSimpleEmailService>(services =>
    {
        var sesOptions = services.GetRequiredService<IOptions<SESOptions>>().Value;
        return new AmazonSimpleEmailServiceClient(
            sesOptions.AccessKey,
            sesOptions.SecretKey,
            RegionEndpoint.GetBySystemName(sesOptions.Region));
    });

    s.AddScoped<PasswordHasher>();
    s.AddScoped<IAccounts, MySqlAccounts>();
    s.AddScoped<IAuthorizationCodes, RedisAuthorizationCodes>();
    s.AddScoped<IEmailVerify, SESEmailVerify>();
    s.AddSingleton<RedisConnection>();
    s.AddHostedService(services => services.GetRequiredService<RedisConnection>());
    return s;
}

async ValueTask StartMigrationAsync(CancellationToken cancellationToken)
{
    var options = app.Services.GetRequiredService<IOptions<MySqlOptions>>();
    var scripts = new Scripts();
    var logger = new LoggerTextWriter(app.Logger);
    await Executor.RunAsync(options.Value.ConnectionString, options.Value.Database, [.. scripts.GetScripts()], logger, cancellationToken);
}

static void ValidateEmailLocalization(IServiceProvider services)
{
    var localizer = services.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Strings>>();
    var originalCulture = System.Globalization.CultureInfo.CurrentUICulture;

    try
    {
        foreach (var cultureName in new[] { "en", "ko", "ja" })
        {
            System.Globalization.CultureInfo.CurrentUICulture =
                System.Globalization.CultureInfo.GetCultureInfo(cultureName);
            var heading = localizer["SEND_EMAILVERIFY_BODY_HTML_HEAD"];
            if (heading.ResourceNotFound)
            {
                throw new InvalidOperationException(
                    $"Email localization resource was not found for culture '{cultureName}'.");
            }
        }
    }
    finally
    {
        System.Globalization.CultureInfo.CurrentUICulture = originalCulture;
    }
}
