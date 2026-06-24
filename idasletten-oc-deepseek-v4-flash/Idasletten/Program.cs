using Idasletten.Features.ScoreSystems;
using Idasletten.Shared;
using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
var dbPath = builder.Configuration.GetConnectionString("Sqlite");
var isMemoryDb = string.IsNullOrEmpty(dbPath) || dbPath == ":memory:";
if (isMemoryDb)
{
    var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
    connection.Open();
    builder.Services.AddSingleton(connection);
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connection));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
}

// Identity
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 1;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Authentication - Azure AD
var azureAdTenantId = builder.Configuration["AzureAd:TenantId"];
var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
if (!string.IsNullOrEmpty(azureAdTenantId) && !string.IsNullOrEmpty(azureAdClientId))
{
    builder.Services.AddAuthentication()
        .AddMicrosoftAccount(microsoftOptions =>
        {
            microsoftOptions.ClientId = azureAdClientId;
            microsoftOptions.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
            microsoftOptions.CallbackPath = "/login";
        });
}

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/login";
});

// Forwarded headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Scoring systems
builder.Services.AddScoped<IScoringSystem, EloScoring>();
builder.Services.AddScoped<IScoringSystem, LivesScoring>();
builder.Services.AddScoped<IScoringSystem, WinCountScoring>();
builder.Services.AddScoped<IScoringSystem, TrueSkillScoring>();

// Authorization
builder.Services.AddAuthorization();

// Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// Forwarded headers
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (isMemoryDb)
    {
        db.Database.EnsureCreated();
    }
    else
    {
        db.Database.Migrate();
    }
}

await SeedData.InitializeAsync(app.Services);

app.Run();

public partial class Program { }
