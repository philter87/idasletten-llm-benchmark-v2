using Idasletten.Shared.Auth;
using Idasletten.Shared.Data;
using Idasletten.Shared.Scoring;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddScoped<ScoreRecalculator>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<GraphAvatarFetcher>();

var testUserOptions = TestUserOptions.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(testUserOptions);

builder.Services.AddAuthorization();
var azureAd = builder.Configuration.GetSection("AzureAd");
var azureAdConfigured = !string.IsNullOrWhiteSpace(azureAd["ClientId"]);
builder.Services.AddSingleton(new AzureAdAvailability(azureAdConfigured));

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    if (azureAdConfigured)
    {
        options.DefaultChallengeScheme = "AzureAD";
    }
});

authBuilder.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});

// Only registered when real credentials are configured: without them, OpenIdConnectOptions
// validation throws on every request (even ones that never touch Azure AD).
if (azureAdConfigured)
{
    authBuilder.AddOpenIdConnect("AzureAD", options =>
    {
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.Authority = $"{azureAd["Instance"]}{azureAd["TenantId"]}/v2.0";
        options.ClientId = azureAd["ClientId"];
        options.ClientSecret = azureAd["ClientSecret"];
        options.CallbackPath = azureAd["CallbackPath"] ?? "/signin-oidc";
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SaveTokens = true;
        options.Scope.Add("User.Read");
        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = AzureAdSignInHandler.HandleTokenValidatedAsync
        };
    });
}

var connectionString = builder.Configuration.GetConnectionString("Default");
var usingLocalInMemoryDatabase = string.IsNullOrWhiteSpace(connectionString);
if (usingLocalInMemoryDatabase)
{
    // No configured connection string: use a single long-lived in-memory SQLite
    // connection so the database survives for the app's lifetime.
    var keepAliveConnection = new SqliteConnection("Data Source=:memory:");
    keepAliveConnection.Open();
    builder.Services.AddSingleton(keepAliveConnection);
    builder.Services.AddDbContext<IdaslettenDbContext>((sp, options) =>
        options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));
}
else
{
    builder.Services.AddDbContext<IdaslettenDbContext>(options =>
        options.UseSqlite(connectionString));
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdaslettenDbContext>();
    db.Database.Migrate();

    if (usingLocalInMemoryDatabase)
    {
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        await DbSeeder.SeedAsync(db, sender, testUserOptions);
    }
}

// Trust Fly.io's proxy so ASP.NET Core generates https:// redirect URIs for Azure AD.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

// Exposed so Idasletten.Tests can boot the app via WebApplicationFactory<Program>.
public partial class Program;
