using Idasletten.Shared.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers for Fly.io proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Database: SQLite in-memory for local dev (no connection string), file-based for production
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? $"DataSource={Path.Combine(builder.Environment.ContentRootPath, "idasletten.db")}";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// Azure AD Authentication
var azureAdConfig = builder.Configuration.GetSection("AzureAd");
if (azureAdConfig.Exists() && !string.IsNullOrEmpty(azureAdConfig["ClientId"]))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{azureAdConfig["TenantId"]}/v2.0";
        options.ClientId = azureAdConfig["ClientId"];
        options.ClientSecret = azureAdConfig["ClientSecret"];
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.CallbackPath = "/signin-oidc";
    });
}
else
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie();
}

var app = builder.Build();

app.UseForwardedHeaders();

// Apply migrations and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!app.Environment.IsEnvironment("Testing"))
    {
        db.Database.Migrate();
        await DatabaseSeeder.SeedAsync(db);

        var testEmail = builder.Configuration["TestUser:Email"];
        var testPassword = builder.Configuration["TestUser:Password"];
        if (!string.IsNullOrEmpty(testEmail) && !string.IsNullOrEmpty(testPassword))
        {
            await DatabaseSeeder.SeedTestUserAsync(db, "TEST", "Test User", testEmail);
        }
    }
}

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
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();

public partial class Program { }
