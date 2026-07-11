using Amazon;
using Amazon.SimpleEmail;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using OAuth2;
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
    s.AddOptions<SESOptions>()
        .Bind(config.GetRequiredSection(nameof(SESOptions)))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    s.AddLocalization(options => options.ResourcesPath = "Localizational");
    s.Configure<RequestLocalizationOptions>(options =>
    {
        string[] supportedCultures = ["en", "ko"];
        options.SetDefaultCulture("en")
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);
    });

    s.AddSingleton<IAmazonSimpleEmailService>(services =>
    {
        var sesOptions = services.GetRequiredService<IOptions<SESOptions>>().Value;
        return new AmazonSimpleEmailServiceClient(
            RegionEndpoint.GetBySystemName(sesOptions.Region));
    });

    s.AddScoped<PasswordHasher>();
    s.AddScoped<IAccounts, MySqlAccounts>();
    s.AddScoped<IEmailVerify, SESEmailVerify>();
    return s;
}

async ValueTask StartMigrationAsync(CancellationToken cancellationToken)
{
    var options = app.Services.GetRequiredService<IOptions<MySqlOptions>>();
    var scripts = new Scripts();
    var logger = new LoggerTextWriter(app.Logger);
    await Executor.RunAsync(options.Value.ConnectionString, options.Value.Database, [.. scripts.GetScripts()], logger, cancellationToken);
}
