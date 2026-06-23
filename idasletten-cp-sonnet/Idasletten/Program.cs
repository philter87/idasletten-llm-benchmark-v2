using Idasletten.Shared.Data;
using Idasletten.Shared.Extensions;
using Idasletten.Shared.Seeding;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Forwarded headers for Fly.io proxy (https)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Database
var isProduction = !builder.Environment.IsDevelopment();
if (isProduction)
{
    var dbPath = Environment.GetEnvironmentVariable("DATABASE_PATH") ?? "idasletten.db";
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite("Data Source=idasletten-dev.db"));
}

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// Authentication
var azureAdSection = builder.Configuration.GetSection("AzureAd");
if (azureAdSection.Exists() && !string.IsNullOrEmpty(azureAdSection["ClientId"]))
{
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
}
else
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options => { options.LoginPath = "/login"; });
}

builder.Services.AddAuthorization();
builder.Services.AddRazorPages().AddMicrosoftIdentityUI();

// Test user auth (only when env vars set)
builder.Services.AddTestUserAuth(builder.Configuration);

// Seeder
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.MapStaticAssets();
    app.MapRazorPages().WithStaticAssets();
}
else
{
    app.MapRazorPages();
}

// Apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();

    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();

public partial class Program { }
