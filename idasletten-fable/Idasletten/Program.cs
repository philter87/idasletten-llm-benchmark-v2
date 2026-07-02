using System.Security.Claims;
using Idasletten.Features.Users;
using Idasletten.Features.Users.Commands;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
// Danish text (æøå) and Old Norse names should not be HTML-entity encoded.
builder.Services.AddSingleton(System.Text.Encodings.Web.HtmlEncoder.Create(System.Text.Unicode.UnicodeRanges.All));
builder.Services.AddHttpClient();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddScoped<IProfileImageProvider, GraphProfileImageProvider>();

// SQLite: in-memory locally (kept alive by one open connection for the app's
// lifetime, so migrations + seeding work), file-based in production.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? (builder.Environment.IsProduction()
        ? "Data Source=idasletten.db"
        : "Data Source=idasletten;Mode=Memory;Cache=Shared");
if (connectionString.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase))
{
    var keepAlive = new SqliteConnection(connectionString);
    keepAlive.Open();
    builder.Services.AddSingleton(keepAlive);
}
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

// Fly.io terminates TLS at its proxy: trust forwarded headers so Azure AD
// redirect URIs are generated with https.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Login is optional; cookies carry the session. Azure AD is only wired up when
// an app registration is configured.
var authentication = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
if (!string.IsNullOrEmpty(azureAdClientId))
{
    authentication.AddOpenIdConnect("AzureAd", options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/v2.0";
        options.ClientId = azureAdClientId;
        options.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
        options.ResponseType = "code";
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.Events.OnTicketReceived = async context =>
        {
            // Make sure every Azure AD login has a matching local user.
            var mediator = context.HttpContext.RequestServices.GetRequiredService<IMediator>();
            var email = context.Principal?.FindFirstValue("preferred_username")
                        ?? context.Principal?.FindFirstValue(ClaimTypes.Email);
            var name = context.Principal?.Identity?.Name ?? email;
            if (name is not null)
            {
                var initials = UserNameHelper.InitialsFrom(name, email);
                await mediator.Send(new CreateUserCommand(initials, name, email));
            }
        };
    });
}

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/login/microsoft", (string? returnUrl) =>
    Results.Challenge(
        new AuthenticationProperties { RedirectUri = returnUrl ?? "/" },
        ["AzureAd"]));

app.MapPost("/logout", async (HttpContext context, string? returnUrl) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect(returnUrl ?? "/");
});

// Migrations are always applied automatically on startup (see AGENTS.md).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}
await SeedData.EnsureSeeded(app.Services, seedDemoData: !app.Environment.IsProduction());

app.Run();

public partial class Program;
