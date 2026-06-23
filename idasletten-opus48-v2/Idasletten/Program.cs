using Idasletten.Data;
using Idasletten.Shared;
using Idasletten.Shared.Events;
using Idasletten.Shared.Graph;
using Idasletten.Shared.Scoring;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Database — SQLite. In-memory locally and in tests (kept alive by a shared open
// connection), file-based in production. Migrations are applied on startup below.
// ---------------------------------------------------------------------------
var useFileDb = builder.Environment.IsProduction();
if (useFileDb)
{
    var conn = builder.Configuration.GetConnectionString("Default") ?? "Data Source=/data/idasletten.db";
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(conn));
}
else
{
    var keepAlive = new SqliteConnection("DataSource=:memory:");
    keepAlive.Open();
    builder.Services.AddSingleton(keepAlive);
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(keepAlive));
}

// ---------------------------------------------------------------------------
// MediatR + the catch-all logging event handler so every domain event has a sink.
// ---------------------------------------------------------------------------
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddTransient(typeof(INotificationHandler<>), typeof(LoggingEventHandler<>));

// Scoring systems.
builder.Services.AddSingleton<IScoreCalculator, EloScoreCalculator>();
builder.Services.AddSingleton<IScoreCalculator, TrueSkillScoreCalculator>();
builder.Services.AddSingleton<IScoreCalculator, LivesScoreCalculator>();
builder.Services.AddSingleton<IScoreCalculator, WinCountScoreCalculator>();
builder.Services.AddScoped<ScoreService>();

// Shared services.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();

// Graph image fetching — real client only when app credentials are configured.
var graphTenant = builder.Configuration["AzureAd:TenantId"];
var graphClient = builder.Configuration["AzureAd:ClientId"];
var graphSecret = builder.Configuration["AzureAd:ClientSecret"];
if (!string.IsNullOrWhiteSpace(graphTenant) && !string.IsNullOrWhiteSpace(graphClient) && !string.IsNullOrWhiteSpace(graphSecret))
{
    builder.Services.AddSingleton(GraphUserImageService.CreateClient(graphTenant, graphClient, graphSecret));
    builder.Services.AddScoped<IUserImageService, GraphUserImageService>();
}
else
{
    builder.Services.AddSingleton<IUserImageService, NullUserImageService>();
}

// ---------------------------------------------------------------------------
// Authentication. Azure AD (Microsoft.Identity.Web) when configured; always a cookie
// scheme so the test-user login works locally and in tests.
// ---------------------------------------------------------------------------
var azureConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["AzureAd:ClientId"])
                      && !string.IsNullOrWhiteSpace(builder.Configuration["AzureAd:Instance"]);
if (azureConfigured)
{
    builder.Services
        .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
}
else
{
    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(o =>
        {
            o.LoginPath = "/login";
            o.AccessDeniedPath = "/login";
        });
}
builder.Services.AddAuthorization();

builder.Services.AddRazorPages();

// Trust the Fly.io proxy so generated redirect URIs are https.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

// ---------------------------------------------------------------------------
// Apply migrations and seed on startup (rule: migrations auto-apply — see AGENTS.md).
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DataSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

// Exposed so the test project's WebApplicationFactory can target this entry point.
public partial class Program;
