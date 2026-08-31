using Idasletten;
using Idasletten.Auth;
using Idasletten.Data;
using Idasletten.Scoring;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// ---------- Forwarded headers (Fly.io proxy) ----------
// Known networks/proxies are intentionally cleared so the proxy's X-Forwarded-*
// headers are trusted and Azure AD gets https:// redirect URIs.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.All;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// ---------- Database ----------
var dbMode = builder.Configuration["Database:Mode"] ?? (builder.Environment.IsDevelopment() ? "Memory" : "File");
var useInMemory = string.Equals(dbMode, "Memory", StringComparison.OrdinalIgnoreCase);

Microsoft.Data.Sqlite.SqliteConnection? inMemoryConnection = null;
if (useInMemory)
{
    // Keep a single open connection for the process lifetime.
    inMemoryConnection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
    inMemoryConnection.Open();
    builder.Services.AddSingleton(inMemoryConnection);
}
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (inMemoryConnection is not null)
    {
        options.UseSqlite(inMemoryConnection);
    }
    else
    {
        var cs = builder.Configuration.GetConnectionString("Idasletten") ?? "Data Source=idasletten.db";
        options.UseSqlite(cs);
    }
});

// ---------- CQRS (MediatR) ----------
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// ---------- Scoring ----------
builder.Services.AddScoped<IScoringEngine, EloScoring>();
builder.Services.AddScoped<IScoringEngine, TrueSkillScoring>();
builder.Services.AddScoped<IScoringEngine, LivesScoring>();
builder.Services.AddScoped<IScoringEngine, WinCountScoring>();
builder.Services.AddScoped<ScoringEngine>();

// ---------- Graph ----------
builder.Services.AddHttpClient<GraphAvatarService>();
// ITokenAcquisition only exists when Azure AD is configured, so resolve it optionally.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenAcquirer>(sp => new TokenAcquirer(
    sp.GetService<ITokenAcquisition>(),
    sp.GetRequiredService<IConfiguration>(),
    sp.GetRequiredService<IHttpClientFactory>()));
builder.Services.AddScoped(sp => sp.GetRequiredService<GraphAvatarService>().AsGraphAvatarService());

// ---------- Authentication ----------
var hasAzureAd = !string.IsNullOrWhiteSpace(builder.Configuration["AzureAd:ClientId"]);
var authBuilder = builder.Services
    .AddAuthentication(AuthConstants.AppScheme)
    .AddCookie(AuthConstants.AppScheme, o =>
    {
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/login";
        o.ExpireTimeSpan = TimeSpan.FromDays(14);
        o.SlidingExpiration = true;
    });
if (hasAzureAd)
{
    // Azure AD (app registration). Sign-in issues the app cookie (AppCookie),
    // so the app is scheme-agnostic after the OIDC round trip.
    authBuilder
        .AddMicrosoftIdentityWebApp(
            builder.Configuration.GetSection("AzureAd"),
            openIdConnectScheme: AuthConstants.AzureAdScheme,
            cookieScheme: AuthConstants.AppScheme);
}
builder.Services.Configure<TestUserOptions>(builder.Configuration.GetSection(TestUserOptions.Section));
builder.Services.AddAuthorization(o =>
    o.AddPolicy(AuthConstants.IdentityPolicy, p => p.RequireAuthenticatedUser()));

// ---------- Razor Pages ----------
builder.Services.AddRazorPages();

var app = builder.Build();

// ---------- Startup: migrations (always) + seeding (local in-memory / tests) ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    var seedEnabled = useInMemory || app.Configuration.GetValue("Seed:Enabled", false);
    if (seedEnabled)
    {
        await SeedData.SeedAsync(scope.ServiceProvider);
    }
}

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();

public partial class Program { }
